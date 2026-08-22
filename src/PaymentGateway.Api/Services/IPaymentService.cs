using PaymentGateway.Api.Common;
using PaymentGateway.Api.DTOs;

namespace PaymentGateway.Api.Services;

public interface IPaymentService
{
    Task<ServiceResult<PaymentResponse>> AuthorizeAsync(Guid merchantId, CreatePaymentRequest paymentRequest);
    Task<ServiceResult<PaymentResponse>> GetByIdAsync(Guid merchantId, Guid paymentId);
    Task<ServiceResult<PaymentResponse>> CaptureAsync(Guid merchantId, Guid paymentId);
}