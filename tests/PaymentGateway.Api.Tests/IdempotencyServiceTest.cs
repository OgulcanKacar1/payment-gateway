using Microsoft.EntityFrameworkCore;
using PaymentGateway.Api.Data;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class IdempotencyServiceTest
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetAsync_UnKnownKey_ReturnsNull()
    {
        //Arrange
        await using var db = CreateInMemoryDb();
        var service = new IdempotencyService(db);
        
        //Act
        var result = await service.GetAsync(Guid.NewGuid(), "bilinmeyen-key");
        
        //Assert: yok ise null dönmeli
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_ReturnsStoredRecord()
    {
        //Arrange
        await using var db = CreateInMemoryDb();
        var service = new IdempotencyService(db);
        var merchantId = Guid.NewGuid();
        var key = "abc-123";
        
        //Act: kaydet sonra aynı key ile getir
        await service.SaveAsync(merchantId, key, 200, "{\"success\":true}");
        var result = await service.GetAsync(merchantId, key);
        
        //Assert: kayıt bulunmalı ve doğru değerleri içermeli
        Assert.NotNull(result);
        Assert.Equal(200, result!.StatusCode);
        Assert.Equal("{\"success\":true}", result.ResponseBody);
    }
}