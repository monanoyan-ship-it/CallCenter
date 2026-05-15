using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-winback")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnWinback)]
public class SlnWinbackController : ControllerBase
{
    private readonly ISlnWinbackFactory _factory;

    public SlnWinbackController(ISlnWinbackFactory factory) => _factory = factory;

    [HttpGet]
    public async Task<ActionResult<List<SlnWinbackRuleDto>>> GetRules([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetRulesAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SlnWinbackRuleDto>> GetRule(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var rule = await _factory.GetRuleAsync(id, customerId, GetBranchId() ?? branchId);
        return rule != null ? Ok(rule) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<SlnWinbackRuleDto>> CreateRule([FromBody] SlnWinbackRuleCreateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.CreateRuleAsync(dto, customerId, GetBranchId() ?? branchId));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateRule(int id, [FromBody] SlnWinbackRuleUpdateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.UpdateRuleAsync(id, dto, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteRule(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.DeleteRuleAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("{id:int}/toggle")]
    public async Task<ActionResult> ToggleRule(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.ToggleRuleAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpGet("{id:int}/preview")]
    public async Task<ActionResult<SlnWinbackPreviewDto>> Preview(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var preview = await _factory.GetPreviewAsync(id, customerId, GetBranchId() ?? branchId);
        return preview != null ? Ok(preview) : NotFound();
    }

    [HttpPost("{id:int}/create-campaign")]
    public async Task<ActionResult<SlnCampaignDto>> CreateCampaign(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (campaign, error) = await _factory.CreateCampaignFromRuleAsync(id, customerId, GetBranchId() ?? branchId);
        return campaign != null ? Ok(campaign) : BadRequest(error);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var value = User.FindFirst("BranchId")?.Value;
        return int.TryParse(value, out var branchId) && branchId > 0 ? branchId : null;
    }
}
