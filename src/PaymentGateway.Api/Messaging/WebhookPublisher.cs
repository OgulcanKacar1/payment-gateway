using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace PaymentGateway.Api.Messaging;

public class WebhookPublisher : IWebhookPublisher
{
    public const string QueueName = "webhook_queue";
    private readonly IRabbitMqConnection _connection;
    
    public WebhookPublisher(IRabbitMqConnection connection)
    {
        _connection = connection;
    }

    public async Task PublishAsync(WebhookMessage message, CancellationToken cancellationToken = default)
    {
        var connection = await _connection.GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken : cancellationToken);
        
        // Kuyruğu garanti et, eğer yoksa oluştur
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true, //RabbitMq sunucusu yeniden başlatılsa bile kuyruk ve mesajlar kaybolmaz
            exclusive: false,
            autoDelete: false,
            cancellationToken : cancellationToken);
        
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json); //RabbitMq mesajları byte[] olarak gönderir
        
        var properties = new BasicProperties
        {
            Persistent = true   // mesaj disk'te kalıcı — durable kuyrukla eşleşir, RabbitMQ çökse bile mesaj durur
        };

        // Mesajı kuyruğa fırlat
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: QueueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

    }
}