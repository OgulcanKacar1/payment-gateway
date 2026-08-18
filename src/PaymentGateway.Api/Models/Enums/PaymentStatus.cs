namespace PaymentGateway.Api.Models.Enums;
public enum PaymentStatus
{
    Pending = 1,
    Authorized = 2,
    Captured = 3,
    Refunded = 4,
    Voided = 5,
    Failed = 6
}