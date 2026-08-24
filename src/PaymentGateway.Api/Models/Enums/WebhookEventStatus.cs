namespace PaymentGateway.Api.Models.Enums;

public enum WebhookEventStatus
{
    Pending = 1, // henüz gönderilmedi veya tekrar denenecek
    Delivered = 2, // başarılı bir şekilde gönderildi
    Failed = 3 // maksimum deneme sayısına ulaşıldı ve başarısız oldu
}