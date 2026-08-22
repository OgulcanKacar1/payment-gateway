namespace PaymentGateway.Api.Models.Entities;

public class IdempotencyRecord : BaseEntity
{
    public Guid MerchantId { get; set; } // Hangi merchant'a ait
    public required string Key { get; set; } // Idempotency-Key
    public int StatusCode { get; set; } // HTTP status code
    
    public required string ResponseBody { get; set; } // Saklanan JSON cevap
}