using PaymentGateway.Api.Common;

namespace PaymentGateway.Api.Tests;

public class CardValidatorTests
{
    [Theory]
    [InlineData("4242424242424242")]   // başarılı test kartı
    [InlineData("4000000000000002")]   // reddedilen ama Luhn-geçerli
    [InlineData("4242 4242 4242 4242")] // boşluklu da geçmeli
    public void IsValidLuhn_ValidCards_ReturnsTrue(string cardNumber)
    {
        Assert.True(CardValidator.IsValidLuhn(cardNumber));
    }

    [Theory]
    [InlineData("1234567890123456")] // geçersiz kart numarası
    [InlineData("4242424242424243")] // geçersiz kart numarası
    public void IsValidLuhn_InvalidCard_ReturnsFalse(string cardNumber)
    {
        Assert.False(CardValidator.IsValidLuhn(cardNumber));
    }
}