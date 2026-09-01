using RabbitMQ.Client;

namespace PaymentGateway.Api.Messaging;

public interface IRabbitMqConnection
{
    // Paylaşılan tek bağlantıyı verir (yoksa açar)
    Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
    
}