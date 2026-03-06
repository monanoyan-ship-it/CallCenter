using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor,CustomerUser")]
public class ReportsController : ControllerBase
{
    private readonly IReportFactory _reportFactory;

    public ReportsController(IReportFactory reportFactory)
    {
        _reportFactory = reportFactory;
    }

    private bool IsSystemAdmin => User.IsInRole("Admin") || User.IsInRole("Supervisor");

    private int? ResolveCustomerId(int? queryCustomerId)
    {
        if (IsSystemAdmin)
            return queryCustomerId;
        var claim = User.FindFirstValue("CustomerId");
        return claim != null ? int.Parse(claim) : null;
    }

    private void StripCallDetails(CallReportResponse report)
    {
        if (!IsSystemAdmin) return;
        report.Items = new() { Items = new(), TotalCount = report.Items.TotalCount, Page = report.Items.Page, PageSize = report.Items.PageSize };
    }

    /// <summary>
    /// Arama raporlari — ozet istatistikler + SLA metrikleri + sayfalamali arama listesi.
    /// </summary>
    [HttpGet("calls")]
    public async Task<ActionResult<CallReportResponse>> GetCallReport(
        [FromQuery] int? customerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? directionId,
        [FromQuery] int? statusId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _reportFactory.GetCallReportAsync(ResolveCustomerId(customerId), from, to, directionId, statusId, page, pageSize);
        StripCallDetails(result);
        return Ok(result);
    }

    /// <summary>
    /// Temsilci performans raporu — ozet + sayfalamali temsilci listesi.
    /// </summary>
    [HttpGet("agents")]
    public async Task<ActionResult<AgentReportResponse>> GetAgentReport(
        [FromQuery] int? customerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        return Ok(await _reportFactory.GetAgentReportAsync(ResolveCustomerId(customerId), from, to, page, pageSize));
    }

    /// <summary>
    /// Kuyruk performans raporu — SLA metrikleri ile.
    /// </summary>
    [HttpGet("queues")]
    public async Task<ActionResult<QueueReportResponse>> GetQueueReport(
        [FromQuery] int? customerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        return Ok(await _reportFactory.GetQueueReportAsync(ResolveCustomerId(customerId), from, to, page, pageSize));
    }

    /// <summary>
    /// Gunluk arama trendi (line chart verisi).
    /// </summary>
    [HttpGet("calls/trend")]
    public async Task<ActionResult<List<DailyCallStatsDto>>> GetCallTrend(
        [FromQuery] int? customerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        return Ok(await _reportFactory.GetDailyTrendAsync(ResolveCustomerId(customerId), from, to));
    }

    /// <summary>
    /// Arama durum dagilimi (pie chart verisi).
    /// </summary>
    [HttpGet("calls/distribution")]
    public async Task<ActionResult<List<StatusDistributionDto>>> GetCallDistribution(
        [FromQuery] int? customerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        return Ok(await _reportFactory.GetStatusDistributionAsync(ResolveCustomerId(customerId), from, to));
    }

    /// <summary>
    /// Arama raporu dosya indirme (CSV veya Excel).
    /// </summary>
    [HttpGet("calls/export")]
    public async Task<IActionResult> ExportCallReport(
        [FromQuery] string format,
        [FromQuery] int? customerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? directionId,
        [FromQuery] int? statusId)
    {
        // Sistem adminleri numara iceren dosya indiremez
        if (IsSystemAdmin)
            return BadRequest(new { message = "Sistem yoneticileri musteri arama detaylarini disari aktaramaz." });

        var resolved = ResolveCustomerId(customerId);
        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await _reportFactory.ExportCallReportExcelAsync(resolved, from, to, directionId, statusId);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"arama-raporu-{DateTime.Now:yyyyMMdd}.xlsx");
        }
        else
        {
            var bytes = await _reportFactory.ExportCallReportCsvAsync(resolved, from, to, directionId, statusId);
            return File(bytes, "text/csv", $"arama-raporu-{DateTime.Now:yyyyMMdd}.csv");
        }
    }

    /// <summary>
    /// Temsilci raporu dosya indirme (CSV veya Excel).
    /// </summary>
    [HttpGet("agents/export")]
    public async Task<IActionResult> ExportAgentReport(
        [FromQuery] string format,
        [FromQuery] int? customerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var resolved = ResolveCustomerId(customerId);
        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await _reportFactory.ExportAgentReportExcelAsync(resolved, from, to);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"temsilci-raporu-{DateTime.Now:yyyyMMdd}.xlsx");
        }
        else
        {
            var bytes = await _reportFactory.ExportAgentReportCsvAsync(resolved, from, to);
            return File(bytes, "text/csv", $"temsilci-raporu-{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}
