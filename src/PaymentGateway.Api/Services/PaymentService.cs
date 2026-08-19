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
            Status = status
        };
        
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        var response = new PaymentResponse
        {
            Id = payment.Id,
            Amount = payment.Amount,
            Currency = payment.Currency,
            CardLast4 = payment.CardLast4,
            Status = payment.Status.ToString(),
            CreatedAt = payment.CreatedAt
        };
        
        return ServiceResult<PaymentResponse>.Success(response);
    }
}