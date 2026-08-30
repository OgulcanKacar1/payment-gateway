using PaymentGateway.Api.Common;
using PaymentGateway.Api.Models.Entities;
using StackExchange.Redis;

namespace PaymentGateway.Api.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private const int RequestLimit = 5; // Pencere başına istek sayısı
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(60); // Pencere süresi
    
    public RateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConnectionMultiplexer redis)
    {
        // Merchant yok ise (auth'suz /v1 sıra dışı istekler) için rate limit uygulanmaz
        if (context.Items["Merchant"] is not Merchant merchant)
        {
            await _next(context);
            return;
        }

        var db = redis.GetDatabase();
        var key = $"rate_limit:{merchant.Id}";
        
        var count = await db.StringIncrementAsync(key); //INCR: yoksa 0 dan başlar, varsa 1 artırır
        if (count == 1)
            await db.KeyExpireAsync(key, Window); // ilk istekte 60 saniyelik pencere başlatılır

        if (count > RequestLimit)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail("Rate limit exceeded. Please try again later."));
            return;
        }
        
        await _next(context);
            
    }
}