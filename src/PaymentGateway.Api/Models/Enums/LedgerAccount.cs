namespace PaymentGateway.Api.Models.Enums;

public enum LedgerAccount
{
    MerchantBalance = 0, // Merchant'a borçlandığımız/ödeyeceğimiz tutar
    Clearing = 1, // kart ağı tarafı, dengeleyici karşı hesap
}