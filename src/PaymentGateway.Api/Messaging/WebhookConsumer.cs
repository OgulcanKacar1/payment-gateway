using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Api.Data;
using PaymentGateway.Api.Models.Entities;
using PaymentGateway.Api.Services;
using RabbitMQ.Client.Events;

namespace PaymentGateway.Api.Messaging;

public class WebhookConsumer : BackgroundService
{
    private readonly IRabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookConsumer> _logger;
    
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
                    
                    if(message is not null)
                        await HandleMessageAsync(message, stoppingToken);
                    
                    //Başrılı, işlenmiş mesajı kuyruktan sil
                    await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple:false, cancellationToken: stoppingToken);
                }
                catch
                {
                    //Hata -> mesajı kuyruğa sokma, at (basit yaklaşım, daha sonra DLQ veya retry mekanizması eklenebilir)
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
    
    private async Task HandleMessageAsync(WebhookMessage message, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope(); //background servis + DI scope köprüsü
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IWebhookSender>();
        
        var merchant = await db.Merchants.FirstOrDefaultAsync(m => m.Id == message.MerchantId, stoppingToken);
        if (merchant is null)
            return; //merchant yoksa mesajı at
        
        // sendasync bir webhook mesajı istiyor ama sadece payloadı kullanıyor -> hafif bir tane kur
        var webhookEvent = new WebhookEvent
        {
            MerchantId = message.MerchantId,
            Payload = message.Payload,
            EventType = message.EventType
        };
        
        await sender.SendAsync(merchant, webhookEvent);
    }
}