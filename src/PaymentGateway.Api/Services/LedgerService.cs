using Microsoft.EntityFrameworkCore;
using PaymentGateway.Api.Data;
using PaymentGateway.Api.DTOs;
using PaymentGateway.Api.Models.Entities;
using PaymentGateway.Api.Models.Enums;

namespace PaymentGateway.Api.Services;

public class LedgerService : ILedgerService
{
    private readonly AppDbContext _db;
    
    public LedgerService(AppDbContext db)
    {
        _db = db;
    }
    
    public void RecordCapture(Payment payment) =>
        Post(payment, payment.Amount, "Payment Captured");
    
    public void RecordRefund(Payment payment) =>
        Post(payment, -payment.Amount, "Payment Refunded");

    public async Task<IReadOnlyList<MerchantBalanceResponse>> GetBalancesAsync(Guid merchantId)
    {
        return await _db.LedgerEntries
            .Where(e => e.MerchantId == merchantId && e.Account == LedgerAccount.MerchantBalance)
            .GroupBy(e => e.Currency)
            .Select(g => new MerchantBalanceResponse
            {
                Currency = g.Key,
                Balance = g.Sum(e => e.Amount)
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SettlementReportItem>> GetSettlementAsync(Guid merchantId, DateOnly date)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); // Günün başlangıcı UTC
        var end = start.AddDays(1);
        
        return await _db.LedgerEntries
            .Where(e => e.MerchantId == merchantId
                        && e.Account == LedgerAccount.MerchantBalance
                        && e.CreatedAt >= start && e.CreatedAt < end)
            .GroupBy(e => e.Currency)
            .Select(g => new SettlementReportItem
            {
                Currency = g.Key,
                TotalCaptured = g.Sum(e => e.Amount > 0 ? e.Amount : 0m),
                TotalRefunded = g.Sum(e => e.Amount < 0 ? -e.Amount : 0m),
                Net = g.Sum(e => e.Amount),
                EntryCount = g.Count()
            })
            .ToListAsync();
    }
    
    //Dengeli çift kayıt: MerchantBalance = delta, Clearing = -delta -> toplam 0
    public void Post(Payment payment, decimal merchantDelta, string description)
    {
        var transactionId = Guid.NewGuid();

        _db.LedgerEntries.Add(new LedgerEntry
        {
            TransactionId = transactionId,
            Account = LedgerAccount.MerchantBalance,
            MerchantId = payment.MerchantId,
            PaymentId = payment.Id,
            Amount = merchantDelta,
            Currency = payment.Currency,
            Description = description,
        });

        _db.LedgerEntries.Add(new LedgerEntry
        {
            TransactionId = transactionId,
            Account = LedgerAccount.Clearing,
            MerchantId = payment.MerchantId,
            PaymentId = payment.Id,
            Amount = -merchantDelta,
            Currency = payment.Currency,
            Description = description,
        });
    }
}