namespace PaymentGateway.Api.Messaging;

public interface IWebhookPublisher
{
    Task PublishAsync(WebhookMessage message, CancellationToken cancellationToken = default);
}