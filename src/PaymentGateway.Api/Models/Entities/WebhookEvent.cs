using PaymentGateway.Api.Models.Enums;

namespace PaymentGateway.Api.Models.Entities;

public class WebhookEvent : BaseEntity
{
    public Guid MerchantId { get; set; } // Hangi merchant'a ait
    public Guid PaymentId { get; set; } // Hangi ödeme ile ilgili
    public required string EventType { get; set; } // Örnek: payment.succeeded, payment.failed
    public required string Payload { get; set; } // Saklanan JSON
    public WebhookEventStatus Status { get; set; } = WebhookEventStatus.Pending; // Gönderim durumu
    public int AttemptCount { get; set; } // Gönderim deneme sayısı
    public DateTime NextAttemptAt { get; set; } // Bir sonraki deneme zamanı
}