using PaymentGateway.Api.Models.Entities;

namespace PaymentGateway.Api.Services;

public interface IIdempotencyService
{
    Task<IdempotencyRecord?> GetAsync(Guid merchantId, string key);
    Task SaveAsync(Guid merchantId, string key, int statusCode, string responseBody);
}