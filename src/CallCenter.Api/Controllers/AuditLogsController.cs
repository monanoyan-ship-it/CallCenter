using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogFactory _auditLogFactory;

    public AuditLogsController(IAuditLogFactory auditLogFactory)
    {
        _auditLogFactory = auditLogFactory;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? category = null,
        [FromQuery] string? action = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int? customerId = null)
    {
        var result = await _auditLogFactory.GetAllAsync(page, pageSize, category, action, search, dateFrom, dateTo, customerId);
        return Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? category = null,
        [FromQuery] string? action = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int? customerId = null)
    {
        var bytes = await _auditLogFactory.ExportCsvAsync(category, action, search, dateFrom, dateTo, customerId);
        var fileName = $"audit-logs-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AuditLogDetailDto>> GetById(long id)
    {
        var detail = await _auditLogFactory.GetByIdAsync(id);
        if (detail == null) return NotFound();
        return Ok(detail);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        return Ok(await _auditLogFactory.GetCategoriesAsync());
    }

    [HttpGet("actions")]
    public async Task<ActionResult<List<string>>> GetActions([FromQuery] string? category = null)
    {
        return Ok(await _auditLogFactory.GetActionsAsync(category));
    }
}
