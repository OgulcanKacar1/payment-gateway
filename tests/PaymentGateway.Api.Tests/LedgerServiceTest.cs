using Microsoft.EntityFrameworkCore;
using PaymentGateway.Api.Data;
using PaymentGateway.Api.Models.Entities;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class LedgerServiceTest
{
    //Her test için taze, izole bir bellek içi Db üretir
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new AppDbContext(options);
    }
    
    
    // Test için basit bir ödeme nesnesi üretir
    private static Payment MakePayment(Guid merchantId) => new()
    {
        Id = Guid.NewGuid(),
        MerchantId = merchantId,
        Amount = 100,
        Currency = "TRY",
        CardLast4 = "4242",
    };

    // MerchantBalance ve Clearing hesapları için çift kayıt oluşturur
    [Fact]
    public async Task RecordCapture_IncreasesMerchantBalance()
    {
        await using var db = CreateInMemoryDb();
        var merchantId = Guid.NewGuid();
        var ledger = new LedgerService(db);
        
        ledger.RecordCapture(MakePayment(merchantId));
        await db.SaveChangesAsync();
        
        var balances = await ledger.GetBalancesAsync(merchantId);
        Assert.Equal(100m, balances.Single(b => b.Currency == "TRY").Balance);
    }

    [Fact]
    public async Task RecordRefund_AfterCapture_BalanceIsZero()
    {
        await using var db = CreateInMemoryDb();
        var merchantId = Guid.NewGuid();
        var payment = MakePayment(merchantId);
        var ledger = new LedgerService(db);
        
        ledger.RecordCapture(payment);
        ledger.RecordRefund(payment);
        await db.SaveChangesAsync();
        
        var balances = await ledger.GetBalancesAsync(merchantId);
        Assert.Equal(0m, balances.Single(b => b.Currency == "TRY").Balance);
    }

    [Fact]
    public async Task RecordCapture_ProducesZeroSumTransaction()
    {
        await using var db = CreateInMemoryDb();
        var merchantId = Guid.NewGuid();
        var ledger = new LedgerService(db);
        
        ledger.RecordCapture(MakePayment(merchantId));
        await db.SaveChangesAsync();
        
        var entries = await db.LedgerEntries.ToListAsync();
        Assert.Equal(2, entries.Count); //Çift kayıt: MerchantBalance ve Clearing
        Assert.Equal(0m, entries.Sum(e => e.Amount)); // Toplam 0 olmalı
    }
}