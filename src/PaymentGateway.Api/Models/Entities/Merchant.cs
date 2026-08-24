namespace PaymentGateway.Api.Models.Entities;
public class Merchant: BaseEntity
{
    public required string Name { get; set; }
    public required string ApiKey { get; set; }
    
    public string? WebhookUrl { get; set; } // Webhook URL(opsiyonel)
    public string? WebhookSecret { get; set; } // Webhook secret key
    
    
}