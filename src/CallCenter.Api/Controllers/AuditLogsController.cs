using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly ServiceFactory _factory;

    public AuditLogsController(ServiceFactory factory)
    {
        _factory = factory;
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
        var svc = _factory.CreateAuditLogService();
        var result = await svc.GetAllAsync(page, pageSize, category, action, search, dateFrom, dateTo, customerId);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AuditLogDetailDto>> GetById(long id)
    {
        var svc = _factory.CreateAuditLogService();
        var detail = await svc.GetByIdAsync(id);

        if (detail == null)
            return NotFound();

        return Ok(detail);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var svc = _factory.CreateAuditLogService();
        var categories = await svc.GetCategoriesAsync();
        return Ok(categories);
    }

    [HttpGet("actions")]
    public async Task<ActionResult<List<string>>> GetActions([FromQuery] string? category = null)
    {
        var svc = _factory.CreateAuditLogService();
        var actions = await svc.GetActionsAsync(category);
        return Ok(actions);
    }
}
