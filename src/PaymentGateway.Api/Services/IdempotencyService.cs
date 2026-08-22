using Microsoft.EntityFrameworkCore;
using PaymentGateway.Api.Data;
using PaymentGateway.Api.Models.Entities;

namespace PaymentGateway.Api.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly AppDbContext _db;
    
    public IdempotencyService(AppDbContext db)
    {
        _db = db;
    }
    
    public async Task<IdempotencyRecord?> GetAsync(Guid merchantId, string key)
    {
        return await _db.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.MerchantId == merchantId && r.Key == key);
    }
    
    public async Task SaveAsync(Guid merchantId, string key, int statusCode, string responseBody)
    {
        var record = new IdempotencyRecord
        {
            MerchantId = merchantId,
            Key = key,
            StatusCode = statusCode,
            ResponseBody = responseBody
        };
        
        _db.IdempotencyRecords.Add(record);
        await _db.SaveChangesAsync();
    }
}