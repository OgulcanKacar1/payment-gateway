using Microsoft.EntityFrameworkCore;
using PaymentGateway.Api.Common;
using PaymentGateway.Api.Data;
using PaymentGateway.Api.Models.Entities;
using PaymentGateway.Api.Models.Enums;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentServiceTests
{
    
    //Her test için taze, izole bir bellek içi Db üretir
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CaptureAsync_NonAuthorizedPayment_ReturnsConflict()
    {
        //Arrange: Failed Durumunda bir ödeme(Authorized olmayan) oluştur
        await using var db = CreateInMemoryDb();
        var merchantId = Guid.NewGuid();
        var payment = new Payment
        {
            MerchantId = merchantId,
            Amount = 100,
            Currency = "TRY",
            CardLast4 = "4242",
            Status = PaymentStatus.Failed
        };
        
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        
        var service = new PaymentService(db, new LedgerService(db));
        
        //Act: Failed bir ödemeyi Capture etmeye çalış
        var result = await service.CaptureAsync(merchantId, payment.Id);
        
        //Assert: Conflict dönmeli
        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CaptureAsync_AuthorizedPayment_ReturnsSuccessAndCaptured()
    {
        //Arrange: Authorized bir ödeme oluştur
        await using var db = CreateInMemoryDb();
        var merchantId = Guid.NewGuid();
        var payment = new Payment
        {
            MerchantId = merchantId,
            Amount = 100,
            Currency = "TRY",
            CardLast4 = "4242",
            Status = PaymentStatus.Authorized
        };
        
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        
        var service = new PaymentService(db, new LedgerService(db));
        
        //Act: Authorized bir ödemeyi Capture et
        var result = await service.CaptureAsync(merchantId, payment.Id);
        
        //Assert: Success dönmeli ve status Captured olmalı
        Assert.True(result.IsSuccess);
        Assert.Equal("Captured", result.Data!.Status);
    }

    [Fact]
    public async Task CaptureAsync_NonExistentPayment_ReturnsNotFound()
    {
        // Arrange: Authorized (Captured DEĞİL) → refund edilemez
        await using var db = CreateInMemoryDb();
        var merchantId = Guid.NewGuid();
        var payment = new Payment
        {
            MerchantId = merchantId,
            Amount = 100,
            Currency = "TRY",
            CardLast4 = "4242",
            Status = PaymentStatus.Authorized
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var service = new PaymentService(db, new LedgerService(db));

        // Act
        var result = await service.RefundAsync(merchantId, payment.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
    }
    
    
}