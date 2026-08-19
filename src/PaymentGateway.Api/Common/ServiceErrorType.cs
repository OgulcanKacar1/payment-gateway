namespace PaymentGateway.Api.Common;

public enum ServiceErrorType
{
    None = 0,
    Validation,    // 400 — geçersiz girdi (bozuk kart no vs.)
    Unauthorized,  // 401 — API key yok/yanlış
    NotFound,      // 404 — ödeme bulunamadı
    Conflict       // 409 — geçersiz durum geçişi (refund edilmişi tekrar refund)
}