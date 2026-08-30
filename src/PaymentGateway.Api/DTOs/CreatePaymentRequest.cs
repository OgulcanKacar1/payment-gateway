using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.DTOs;

public class CreatePaymentRequest
{
    [Range(0.01, 999_999_999.99, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }
    
    [Required(ErrorMessage = "Currency is required.")]
    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency must be a 3-letter ISO code (e.g. TRY, USD).")]
    public required string Currency { get; set; } // "TRY", "USD" — 3 harfli ISO kodu
    
    [Required(ErrorMessage = "Card number is required.")]
    [RegularExpression("^[0-9]{13,19}$", ErrorMessage = "Card number must be 13-19 digits.")]
    public required string CardNumber { get; set; } // Kart numarası
}