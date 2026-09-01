using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Api.Data;
using PaymentGateway.Api.Models.Entities;
using PaymentGateway.Api.Services;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;

namespace PaymentGateway.Api.Messaging;

public class WebhookConsumer : BackgroundService
{
    private readonly IRabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookConsumer> _logger;
    
    private const string RetryQueueName = "webhook-retry-queue";
    private const string DeadQueueName = "webhook-dead-queue";
    private const int MaxAttempts = 5;
    private const int RetryDelayMs = 15000;   // 15 sn bekleme
    
    public WebhookConsumer(IRabbitMqConnection connection, IServiceScopeFactory scopeFactory, ILogger<WebhookConsumer> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var connection = await _connection.GetConnectionAsync(stoppingToken);
            var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
            
            //Kuyruğu garanti et
            await channel.QueueDeclareAsync(
                queue: WebhookPublisher.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);
            
            // Retry (bekleme) kuyruğu: mesaj RetryDelayMs kadar bekler, sonra webhook-queue'ya geri döner
            var retryArgs = new Dictionary<string, object?>
            {
                ["x-message-ttl"] = RetryDelayMs,
                ["x-dead-letter-exchange"] = "",                          // varsayılan exchange
                ["x-dead-letter-routing-key"] = WebhookPublisher.QueueName // → webhook-queue
            };
            await channel.QueueDeclareAsync(
                queue: RetryQueueName,
                durable: true, exclusive: false, autoDelete: false,
                arguments: retryArgs,
                cancellationToken: stoppingToken);

            // Dead (mezarlık) kuyruğu: 5 denemede gitmeyen mesajların son durağı
            await channel.QueueDeclareAsync(
                queue: DeadQueueName,
                durable: true, exclusive: false, autoDelete: false,
                cancellationToken: stoppingToken);
            
            //Aynı anda tek mesaj işlenmesini sağlamak için QoS ayarla
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);
            
            var consumer = new AsyncEventingBasicConsumer(channel);
            
            //kuyruğa mesaj düşünce şu event tetiklenir
            consumer.ReceivedAsync += async (sender, eventArgs) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                    var message = JsonSerializer.Deserialize<WebhookMessage>(json);
                    
                    var success = message is null || await HandleMessageAsync(message, stoppingToken);
                    
                    if(!success)
                        await RouteFailedAsync(channel, eventArgs, stoppingToken); // başarısız mesajı retry veya dead kuyruğuna at
                    
                    // Orjinali her durumda Actle: başarılı = bitti , başarısız = retry/dead kuyruğuna atıldı
                    await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple:false, cancellationToken: stoppingToken);
                }
                catch
                {
                    //Hata -> mesajı kuyruğa sokma, at 
                    await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple:false, requeue:false, cancellationToken: stoppingToken);
                }
            };
            
            //Dinlemeyi başlat
            await channel.BasicConsumeAsync(
                queue: WebhookPublisher.QueueName,
                autoAck: false,
                consumerTag: string.Empty,
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: consumer,
                cancellationToken: stoppingToken);
            
            //Servis kapanana kadar dinlemeye devam et
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            //Uygulama Kapanıyor, Sessizce çık
        }
        catch(Exception ex)
        {
            //RabbitMq erişilemiyor -> consumer devre dışı bırak, uygulama çalışmaya devam etsin (fail-open)
            _logger.LogWarning(ex,"Webhook Consumer başlatılamadı: Devre dışı");
        }
    }
    
    private async Task<bool> HandleMessageAsync(WebhookMessage message, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope(); //background servis + DI scope köprüsü
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IWebhookSender>();
        
        var merchant = await db.Merchants.FirstOrDefaultAsync(m => m.Id == message.MerchantId, stoppingToken);
        if (merchant is null)
            return true; //merchant yoksa mesajı at
        
        // sendasync bir webhook mesajı istiyor ama sadece payloadı kullanıyor -> hafif bir tane kur
        var webhookEvent = new WebhookEvent
        {
            MerchantId = message.MerchantId,
            Payload = message.Payload,
            EventType = message.EventType
        };
        
        return await sender.SendAsync(merchant, webhookEvent);
    }
    
    //Başarısız mesajı: deneme<max ise retry kuyruğuna, max deneme ise dead kuyruğuna at
    private async Task RouteFailedAsync(IChannel channel, BasicDeliverEventArgs eventArgs,
        CancellationToken stoppingToken)
    {
        var attempt = GetAttempt(eventArgs) + 1;
        var targetQueue = attempt >= MaxAttempts ? DeadQueueName : RetryQueueName;

        var properties = new BasicProperties
        {
            Persistent = true,
            Headers = new Dictionary<string, object?> { ["x-attempt"] = attempt } // deneme sayısına taşı
        };
        
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: targetQueue,
            mandatory: false,
            basicProperties: properties,
            body: eventArgs.Body, // aynı mesaj gövdesi
            cancellationToken: stoppingToken);
        
        _logger.LogWarning("Webhook gönderilemedi (deneme {Attempt}) -> {Queue}", attempt, targetQueue);
    }
    
    // Mesaj başlığından deneme sayısını oku (yoksa 0)
    private static int GetAttempt(BasicDeliverEventArgs eventArgs)
    {
        if (eventArgs.BasicProperties.Headers is { } headers &&
            headers.TryGetValue("x-attempt", out var value) && value is not null)
        {
            try { return Convert.ToInt32(value); } catch { return 0; }
        }
        return 0;
    }
}