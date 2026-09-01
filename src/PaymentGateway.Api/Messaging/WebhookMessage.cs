namespace PaymentGateway.Api.Messaging;

public class WebhookMessage
{
    public Guid MerchantId { get; set; } // consumer merchantı bunla yükler (URL + secret için)
    public required string EventType { get; set; } // PaymentSucceeded, PaymentFailed, RefundSucceeded, RefundFailed
    public required string Payload { get; set; } // imzalanıp gönderilecek JSON payload
    
}