using PaymentGateway.Api.Models.Entities;

namespace PaymentGateway.Api.Services;

public interface IWebhookSender
{
    Task<bool> SendAsync(Merchant merchant, WebhookEvent webhookEvent);
}