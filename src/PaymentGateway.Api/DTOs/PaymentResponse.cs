namespace PaymentGateway.Api.DTOs;

public class PaymentResponse
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public required string CardLast4 { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}