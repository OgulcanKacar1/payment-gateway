using PaymentGateway.Api.Models.Enums;

namespace PaymentGateway.Api.Models.Entities;

public class Payment : BaseEntity
{
    public Guid MerchantId { get; set; }
    public Merchant Merchant { get; set; } = null!;
    
    public decimal Amount { get; set; }
    public required string Currency { get; set; } // "TRY", "USD" — 3 harfli ISO kodu
    public required string CardLast4 { get; set; } // Kart numarasının son 4 hanesi
    
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
}