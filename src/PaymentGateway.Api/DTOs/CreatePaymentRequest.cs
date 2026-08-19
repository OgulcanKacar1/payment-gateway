namespace PaymentGateway.Api.DTOs;

public class CreatePaymentRequest
{
    public decimal Amount { get; set; }
    public required string Currency { get; set; } // "TRY", "USD" — 3 harfli ISO kodu
    public required string CardNumber { get; set; } // Kart numarası
}