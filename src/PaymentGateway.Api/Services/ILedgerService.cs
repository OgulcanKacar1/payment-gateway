using PaymentGateway.Api.DTOs;
using PaymentGateway.Api.Models.Entities;

namespace PaymentGateway.Api.Services;

public interface ILedgerService
{
    // Kayıtları _db ye EKLER, kaydetmez - çağıran SaveChangesAsync() ile otomatik kaydedilir
    void RecordCapture(Payment payment);
    void RecordRefund(Payment payment);
    
    Task<IReadOnlyList<MerchantBalanceResponse>> GetBalancesAsync(Guid merchantId);
    
    Task<IReadOnlyList<SettlementReportItem>> GetSettlementAsync(Guid merchantId, DateOnly date); 
}