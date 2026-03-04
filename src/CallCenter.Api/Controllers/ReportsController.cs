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
    /// Arama raporlari — ozet istatistikler + sayfalamali arama listesi.
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
}
