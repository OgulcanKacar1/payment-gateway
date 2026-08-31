using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Common;
using PaymentGateway.Api.Models.Entities;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.DTOs;

namespace PaymentGateway.Api.Controllers;

[ApiController]
[Route("v1/reports")]
public class ReportsController: ControllerBase
{
    private readonly ILedgerService _ledger;

    public ReportsController(ILedgerService ledger)
    {
        _ledger = ledger;
    }

    [HttpGet("settlement")]
    public async Task<IActionResult> GetSettlementReport([FromQuery] DateOnly? date)
    {
        var merchant = (Merchant)HttpContext.Items["Merchant"]!;
        var reportDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow); //verilmezse bugünün tarihi
        var report = await _ledger.GetSettlementAsync(merchant.Id, reportDate);
        
        return Ok(ApiResponse<IReadOnlyList<SettlementReportItem>>.Ok(report));
    }
}