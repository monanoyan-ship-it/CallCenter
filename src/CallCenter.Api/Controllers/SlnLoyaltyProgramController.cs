using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-loyalty-programs")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnLoyalty)]
public class SlnLoyaltyProgramController : ControllerBase
{
    private readonly ISlnLoyaltyProgramFactory _factory;

    public SlnLoyaltyProgramController(ISlnLoyaltyProgramFactory factory) => _factory = factory;

    [HttpGet("programs")]
    public async Task<ActionResult<List<SlnLoyaltyProgramDto>>> GetPrograms([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetProgramsAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpPost("programs")]
    public async Task<ActionResult<SlnLoyaltyProgramDto>> CreateProgram([FromBody] SlnLoyaltyProgramCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.CreateProgramAsync(dto, customerId));
    }

    [HttpPut("programs/{id}")]
    public async Task<ActionResult> UpdateProgram(int id, [FromBody] SlnLoyaltyProgramCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.UpdateProgramAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("programs/{id}")]
    public async Task<ActionResult> DeleteProgram(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.DeleteProgramAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpGet("client-progress")]
    public async Task<ActionResult<List<SlnClientLoyaltyProgressDto>>> GetClientProgress([FromQuery] int? clientId, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetClientProgressAsync(customerId, clientId, GetBranchId() ?? branchId));
    }

    [HttpGet("rewards")]
    public async Task<ActionResult<List<SlnLoyaltyProgramRewardDto>>> GetAvailableRewards([FromQuery] int clientId, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        if (clientId <= 0) return BadRequest("clientId zorunludur");
        return Ok(await _factory.GetAvailableRewardsAsync(customerId, clientId, GetBranchId() ?? branchId));
    }

    [HttpPost("rewards/{id}/apply")]
    public async Task<ActionResult> ApplyReward(int id, [FromQuery] int invoiceItemId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        if (invoiceItemId <= 0) return BadRequest("invoiceItemId zorunludur");
        var (success, error) = await _factory.ApplyRewardAsync(customerId, id, invoiceItemId);
        return success ? Ok() : BadRequest(error);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}
