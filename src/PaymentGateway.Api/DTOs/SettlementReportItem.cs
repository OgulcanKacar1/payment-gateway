namespace PaymentGateway.Api.DTOs;

public class SettlementReportItem
{
    public required string Currency { get; set; } // TRY, USD, EUR
    public decimal TotalCaptured { get; set; } // O gün yakalanan toplam tutar - pozitif
    public decimal TotalRefunded { get; set; } // O gün iade edilen toplam tutar - pozitif
    public decimal Net {get; set;} // TotalCaptured - TotalRefunded
    public int EntryCount { get; set; } // O gün yapılan toplam işlem sayısı (yakalama + iade)
}