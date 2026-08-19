using Microsoft.EntityFrameworkCore;
using PaymentGateway.Api.Common;
using PaymentGateway.Api.Data;

namespace PaymentGateway.Api.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (!context.Request.Path.StartsWithSegments("/v1"))
        {
            await _next(context);
            return;
        }
        
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Api Key Gerekli."));
            return;
        } 
        
        var apiKey = authHeader["Bearer ".Length..].Trim();
        var merchant = await db.Merchants.FirstOrDefaultAsync(m => m.ApiKey == apiKey);

        if (merchant is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Gecersiz Api Key."));
            return;
        }
        
        context.Items["Merchant"] = merchant;
        await _next(context);
    }
    
}