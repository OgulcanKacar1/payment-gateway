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
    
    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        var merchant = (Merchant)HttpContext.Items["Merchant"]!;

        var result = await _paymentService.AuthorizeAsync(merchant.Id, request);
        
        if(!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage!));
        
        return Ok(ApiResponse<PaymentResponse>.Ok(result.Data!));
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