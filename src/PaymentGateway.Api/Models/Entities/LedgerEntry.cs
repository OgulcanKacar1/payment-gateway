using PaymentGateway.Api.Models.Enums;

namespace PaymentGateway.Api.Models.Entities;

public class LedgerEntry : BaseEntity
{
    public Guid TransactionId { get; set; } // Bir olayın (capture, refund) kayıtlarını gruplamak için
    public LedgerAccount Account { get; set; } // Hangi hesapta değişiklik oldu
    
    public Guid MerchantId { get; set; } // Kimin defteri , izolasyon + bakiye için
    public Merchant Merchant { get; set; } = null!;
    
    public Guid PaymentId { get; set; } // Hangi ödeme ile ilgili (izlenebilirlik)
    public Payment Payment { get; set; } = null!;
    
    public decimal Amount { get; set; } // İşaretli: + / - (bir işlemde birden fazla entry olabilir, toplamı 0 olmalı)
    public required string Currency { get; set; } // ISO 4217, Merchant'ın para birimi
    public required string Description { get; set; } // Örn: "Capture", "Refund", "Chargeback", "Payout"
    
}