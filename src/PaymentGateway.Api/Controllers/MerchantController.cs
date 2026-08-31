using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Common;
using PaymentGateway.Api.DTOs;
using PaymentGateway.Api.Models.Entities;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[ApiController]
[Route("v1/merchants")]
public class MerchantController : ControllerBase
{
    private readonly ILedgerService _ledger;

    public MerchantController(ILedgerService ledger)
    {
        _ledger = ledger;
    }

    [HttpGet("me/balance")]
    public async Task<IActionResult> GetBalance()
    {
        var merchant = (Merchant)HttpContext.Items["Merchant"]!;
        var balances = await _ledger.GetBalancesAsync(merchant.Id);
        
        return Ok(ApiResponse<IReadOnlyList<MerchantBalanceResponse>>.Ok(balances));
    }
    
}