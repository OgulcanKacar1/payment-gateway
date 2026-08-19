namespace PaymentGateway.Api.Common;

public static class CardValidator
{
    public static bool IsValidLuhn(string cardNumber)
    {
        var sum = 0;
        var isSecond = false;

        for (var i = cardNumber.Length - 1; i >= 0; i--)
        {
            var c = cardNumber[i];
            
            if (!char.IsDigit(c))
            {
                continue; // bosluk veya rakam olmayan karakter varsa geçersiz
            }
            
            var d = c - '0'; // karakteri rakama çevir

            if (isSecond)
            {
                d *= 2;
                if (d > 9)
                    d-= 9;
            }
            
            sum += d;
            isSecond = !isSecond;
        }

        return sum % 10 == 0;
    }
}