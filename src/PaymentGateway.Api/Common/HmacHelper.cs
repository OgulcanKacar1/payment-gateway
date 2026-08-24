using System.Security.Cryptography;
using System.Text;

namespace PaymentGateway.Api.Common;

public static class HmacHelper
{
    public static string ComputeSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }   
}