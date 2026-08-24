using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Api.Common;
using PaymentGateway.Api.Data;
using PaymentGateway.Api.DTOs;
using PaymentGateway.Api.Models.Entities;
using PaymentGateway.Api.Models.Enums;

namespace PaymentGateway.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;

    public PaymentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<PaymentResponse>> AuthorizeAsync(Guid merchantId,
        CreatePaymentRequest request)
    {
        // 1. Tutar geçerli mi?
        if (request.Amount <= 0)
        {
            return ServiceResult<PaymentResponse>.Failure("Tutar 0'dan büyük olmalıdır.", ServiceErrorType.Validation);
        }
        // 2.Kart Numarası geçerli mi? (Luhn algoritması)
        if(!CardValidator.IsValidLuhn(request.CardNumber))
            return ServiceResult<PaymentResponse>.Failure("Geçersiz kart numarası.", ServiceErrorType.Validation);
        
        //3. Sadece rakamları al, son 4 haneyi maskelenmiş şekilde döndür
        var digitsOnly = new string(request.CardNumber.Where(char.IsDigit).ToArray());
        var last4 = digitsOnly[^4..]; // son 4 haneyi al
        
        //4. Test kartı kuralı: durumu belirle
        var status = digitsOnly == "4000000000000002"
            ? PaymentStatus.Failed
            : PaymentStatus.Authorized;

        var payment = new Payment
        {
            MerchantId = merchantId,
            Amount = request.Amount,
            Currency = request.Currency,
            CardLast4 = last4,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
        
        _db.Payments.Add(payment);
        var eventType = status == PaymentStatus.Authorized ? "payment.authorized" : "payment.failed";
        AddWebhookEvent(payment, eventType);
        await _db.SaveChangesAsync();
        
        return ServiceResult<PaymentResponse>.Success(MapToResponse(payment));
    }

    public async Task<ServiceResult<PaymentResponse>> GetByIdAsync(Guid merchantId, Guid paymentId)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.MerchantId == merchantId);

        if (payment == null)
            return ServiceResult<PaymentResponse>.Failure("Ödeme bulunamadı.", ServiceErrorType.NotFound);
        
        return ServiceResult<PaymentResponse>.Success(MapToResponse(payment));
    }

    public async Task<ServiceResult<PaymentResponse>> CaptureAsync(Guid merchantId, Guid paymentId)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.MerchantId == merchantId);
        
        if (payment is null)
            return ServiceResult<PaymentResponse>.Failure("Ödeme bulunamadı", ServiceErrorType.NotFound);
        
        // Durum makinesi: sadece Authorized durumundaki ödemeler Capture edilebilir
        if (payment.Status != PaymentStatus.Authorized)
            return ServiceResult<PaymentResponse>.Failure(
                $"Bu ödeme capture edilemez. Mevcut durum: {payment.Status}", ServiceErrorType.Conflict);
        
        payment.Status = PaymentStatus.Captured;
        AddWebhookEvent(payment, "payment.captured"); // webhook event ekle
        await _db.SaveChangesAsync();
        
        return ServiceResult<PaymentResponse>.Success(MapToResponse(payment));
    }


    public async Task<ServiceResult<PaymentResponse>> VoidAsync(Guid merchantId, Guid paymentId)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.MerchantId == merchantId);
        
        if (payment is null)
            return ServiceResult<PaymentResponse>.Failure("Ödeme bulunamadı", ServiceErrorType.NotFound);
        
        if(payment.Status != PaymentStatus.Authorized)
            return ServiceResult<PaymentResponse>.Failure(
                $"Bu ödeme void edilemez. Mevcut durum: {payment.Status}", ServiceErrorType.Conflict);
        
        payment.Status = PaymentStatus.Voided;
        AddWebhookEvent(payment, "payment.voided");
        await _db.SaveChangesAsync();
        
        return ServiceResult<PaymentResponse>.Success(MapToResponse(payment));
    }

    public async Task<ServiceResult<PaymentResponse>> RefundAsync(Guid merchantId, Guid paymentId)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.MerchantId == merchantId);
        
        if(payment is null)
            return ServiceResult<PaymentResponse>.Failure("Ödeme bulunamadı", ServiceErrorType.NotFound);
        
        if(payment.Status != PaymentStatus.Captured)
            return ServiceResult<PaymentResponse>.Failure(
                $"Bu ödeme refund edilemez. Mevcut durum: {payment.Status}", ServiceErrorType.Conflict);
        
        payment.Status = PaymentStatus.Refunded;
        AddWebhookEvent(payment, "payment.refunded");
        await _db.SaveChangesAsync();
        
        return ServiceResult<PaymentResponse>.Success(MapToResponse(payment));
    }
    
    private static PaymentResponse MapToResponse(Payment payment) => new()
    {
        Id = payment.Id,
        Amount = payment.Amount,
        Currency = payment.Currency,
        CardLast4 = payment.CardLast4,
        Status = payment.Status.ToString(),
        CreatedAt = payment.CreatedAt
    };
    
    private void AddWebhookEvent(Payment payment, string eventType)
    {
        var payload = JsonSerializer.Serialize(new
        {
            eventType,
            payment = MapToResponse(payment)
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            
            _db.WebhookEvents.Add(new WebhookEvent
                {
                    MerchantId = payment.MerchantId,
                    PaymentId = payment.Id,
                    EventType = eventType,
                    Payload = payload,
                    Status = WebhookEventStatus.Pending,
                    NextAttemptAt =  DateTime.UtcNow //hemen gönderilmeye hazır
                    
                }
        );
    }
}