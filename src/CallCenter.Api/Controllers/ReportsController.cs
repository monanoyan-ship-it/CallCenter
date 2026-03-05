using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class ReportsController : ControllerBase
{
    private readonly IReportFactory _reportFactory;

    public ReportsController(IReportFactory reportFactory)
    {
        _reportFactory = reportFactory;
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
        return Ok(await _reportFactory.GetCallReportAsync(customerId, from, to, directionId, statusId, page, pageSize));
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
        return Ok(await _reportFactory.GetAgentReportAsync(customerId, from, to, page, pageSize));
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
        return Ok(await _reportFactory.GetQueueReportAsync(customerId, from, to, page, pageSize));
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
        return Ok(await _reportFactory.GetDailyTrendAsync(customerId, from, to));
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
        return Ok(await _reportFactory.GetStatusDistributionAsync(customerId, from, to));
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
        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await _reportFactory.ExportCallReportExcelAsync(customerId, from, to, directionId, statusId);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"arama-raporu-{DateTime.Now:yyyyMMdd}.xlsx");
        }
        else
        {
            var bytes = await _reportFactory.ExportCallReportCsvAsync(customerId, from, to, directionId, statusId);
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
        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await _reportFactory.ExportAgentReportExcelAsync(customerId, from, to);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"temsilci-raporu-{DateTime.Now:yyyyMMdd}.xlsx");
        }
        else
        {
            var bytes = await _reportFactory.ExportAgentReportCsvAsync(customerId, from, to);
            return File(bytes, "text/csv", $"temsilci-raporu-{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}
