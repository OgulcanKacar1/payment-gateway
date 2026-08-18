namespace PaymentGateway.Api.Models.Entities;
public class Merchant: BaseEntity
{
    public required string Name { get; set; }
    public required string ApiKey { get; set; }
}