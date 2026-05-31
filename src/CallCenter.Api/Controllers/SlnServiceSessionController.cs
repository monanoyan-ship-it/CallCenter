using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-service-sessions")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnSales)]
public class SlnServiceSessionController : ControllerBase
{
    private readonly ISlnServiceSessionFactory _factory;

    public SlnServiceSessionController(ISlnServiceSessionFactory factory) => _factory = factory;

    [HttpGet("plans")]
    public async Task<ActionResult<List<SlnServiceSessionPlanDto>>> GetPlans(
        [FromQuery] int? clientId,
        [FromQuery] int? branchId,
        [FromQuery] bool activeOnly = false)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _factory.GetPlansAsync(customerId, clientId, GetBranchId() ?? branchId, activeOnly));
    }

    [HttpPost("use")]
    public async Task<ActionResult<SlnServiceSessionRecordDto>> UseSession([FromBody] SlnServiceSessionUseDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (record, error) = await _factory.RecordSessionAsync(dto, GetPersonnelId(), customerId, GetBranchId() ?? branchId);
        return record != null ? Ok(record) : BadRequest(error);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int GetPersonnelId()
        => int.Parse(User.FindFirst("CustomerPersonnelId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}
