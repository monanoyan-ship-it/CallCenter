using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-reports")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnReports)]
public class SlnReportController : ControllerBase
{
    private readonly ISlnReportFactory _reportFactory;

    public SlnReportController(ISlnReportFactory reportFactory) => _reportFactory = reportFactory;

    [HttpGet("sales")]
    public async Task<ActionResult<SlnSalesReportDto>> GetSalesReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var report = await _reportFactory.GetSalesReportAsync(customerId, from, to, GetBranchId());
        return Ok(report);
    }

    [HttpGet("staff")]
    public async Task<ActionResult<SlnStaffReportDto>> GetStaffReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var report = await _reportFactory.GetStaffReportAsync(customerId, from, to, GetBranchId());
        return Ok(report);
    }

    [HttpGet("stock")]
    public async Task<ActionResult<SlnStockReportDto>> GetStockReport()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var report = await _reportFactory.GetStockReportAsync(customerId);
        return Ok(report);
    }

    [HttpGet("finance")]
    public async Task<ActionResult<SlnFinanceReportDto>> GetFinanceReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var report = await _reportFactory.GetFinanceReportAsync(customerId, from, to, GetBranchId());
        return Ok(report);
    }

    [HttpGet("clients")]
    public async Task<ActionResult<SlnClientReportDto>> GetClientReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var report = await _reportFactory.GetClientReportAsync(customerId, from, to, GetBranchId());
        return Ok(report);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}
