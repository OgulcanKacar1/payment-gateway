namespace PaymentGateway.Api.DTOs;

public class MerchantBalanceResponse
{
    public required string Currency { get; set; }
    public decimal Balance { get; set; }
}