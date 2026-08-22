using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Common;
using PaymentGateway.Api.DTOs;
using PaymentGateway.Api.Models.Entities;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[ApiController]
[Route("v1/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IIdempotencyService _idempotencyService;
    
    public PaymentController(IPaymentService paymentService, IIdempotencyService idempotencyService)
    {
        _paymentService = paymentService;
        _idempotencyService = idempotencyService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        var merchant = (Merchant)HttpContext.Items["Merchant"]!;
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        var hasKey = !string.IsNullOrWhiteSpace(idempotencyKey);

        if (hasKey)
        {
            var existing = await _idempotencyService.GetAsync(merchant.Id, idempotencyKey);
            if (existing is not null)
                return new ContentResult
                {
                    StatusCode = existing.StatusCode,
                    Content = existing.ResponseBody,
                    ContentType = "application/json"
                };
        }

        var result = await _paymentService.AuthorizeAsync(merchant.Id, request);

        int statusCode;
        object body;

        if (result.IsSuccess)
        {
            statusCode = StatusCodes.Status200OK;
            body = ApiResponse<PaymentResponse>.Ok(result.Data!);
        }
        else
        {
            statusCode = StatusCodes.Status400BadRequest;
            body = ApiResponse<object>.Fail(result.ErrorMessage!);
        }
        
        // key varsa cevabı kaydet(bir sonraki isteklerde aynı cevabı döndürmek için)
        if (hasKey)
        {
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            await _idempotencyService.SaveAsync(merchant.Id, idempotencyKey, statusCode, json);
        }
        
        return StatusCode(statusCode, body);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var merchant = (Merchant)HttpContext.Items["Merchant"]!;
        
        var result = await _paymentService.GetByIdAsync(merchant.Id, id);
        
        if(result.IsSuccess)
            return Ok(ApiResponse<PaymentResponse>.Ok(result.Data!));

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound =>
                NotFound(ApiResponse<object>.Fail(result.ErrorMessage!)),
            _ => BadRequest(ApiResponse<object>.Fail(result.ErrorMessage!))
        };
    }

    [HttpPost("{id}/capture")]
    public async Task<IActionResult> Capture(Guid id)
    {
        var merchant = (Merchant)HttpContext.Items["Merchant"]!;
        
        var result = await _paymentService.CaptureAsync(merchant.Id, id);
        
        if(result.IsSuccess)
            return Ok(ApiResponse<PaymentResponse>.Ok(result.Data!));

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound =>
                NotFound(ApiResponse<object>.Fail(result.ErrorMessage!)),
            ServiceErrorType.Conflict =>
                Conflict(ApiResponse<object>.Fail(result.ErrorMessage!)),
            _ => BadRequest(ApiResponse<object>.Fail(result.ErrorMessage!))
        };
    }

    [HttpPost("{id}/void")]
    public async Task<IActionResult> Void(Guid id)
    {
        var merchant = (Merchant)HttpContext.Items["Merchant"]!;
        
        var result = await _paymentService.VoidAsync(merchant.Id, id);
        
        if(result.IsSuccess)
            return Ok(ApiResponse<PaymentResponse>.Ok(result.Data!));

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound =>
                NotFound(ApiResponse<object>.Fail(result.ErrorMessage!)),
            ServiceErrorType.Conflict =>
                Conflict(ApiResponse<object>.Fail(result.ErrorMessage!)),
            _ => BadRequest(ApiResponse<object>.Fail(result.ErrorMessage!))
        };

    }
    
    [HttpPost("{id}/refund")]
    public async Task<IActionResult> Refund(Guid id)
    {
        var merchant = (Merchant)HttpContext.Items["Merchant"]!;
        
        var result = await _paymentService.RefundAsync(merchant.Id, id);
        
        if(result.IsSuccess)
            return Ok(ApiResponse<PaymentResponse>.Ok(result.Data!));

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound =>
                NotFound(ApiResponse<object>.Fail(result.ErrorMessage!)),
            ServiceErrorType.Conflict =>
                Conflict(ApiResponse<object>.Fail(result.ErrorMessage!)),
            _ => BadRequest(ApiResponse<object>.Fail(result.ErrorMessage!))
        };
    }
    
}