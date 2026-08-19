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
}