using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-marketing")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnCampaigns)]
public class SlnMarketingController : ControllerBase
{
    private readonly ISlnMarketingFactory _marketingFactory;
    private const string BranchTargetRequiredMessage = "Sube secin veya Tum Subeler secenegini secin";

    public SlnMarketingController(ISlnMarketingFactory marketingFactory) => _marketingFactory = marketingFactory;

    // ═══ Kampanya ═══

    [HttpGet("campaigns")]
    public async Task<ActionResult<List<SlnCampaignDto>>> GetCampaigns([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var campaigns = await _marketingFactory.GetCampaignsAsync(customerId, GetBranchId() ?? branchId);
        return Ok(campaigns);
    }

    [HttpGet("campaigns/{id:int}")]
    public async Task<ActionResult<SlnCampaignDto>> GetCampaign(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var campaign = await _marketingFactory.GetCampaignAsync(id, customerId, GetBranchId() ?? branchId);
        return campaign != null ? Ok(campaign) : NotFound();
    }

    [HttpPost("campaigns")]
    public async Task<ActionResult<SlnCampaignDto>> CreateCampaign([FromBody] SlnCampaignCreateDto dto, [FromQuery] int? branchId, [FromQuery] bool allBranches = false)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveMutationBranchTarget(branchId, allBranches);
        if (target.Error != null) return target.Error;

        var campaign = await _marketingFactory.CreateCampaignAsync(dto, customerId, target.BranchId);
        return Ok(campaign);
    }

    [HttpPut("campaigns/{id:int}")]
    public async Task<ActionResult> UpdateCampaign(int id, [FromBody] SlnCampaignUpdateDto dto, [FromQuery] int? branchId, [FromQuery] bool allBranches = false)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveMutationBranchTarget(branchId, allBranches);
        if (target.Error != null) return target.Error;

        var (success, error) = await _marketingFactory.UpdateCampaignAsync(id, dto, customerId, target.BranchId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("campaigns/{id:int}")]
    public async Task<ActionResult> DeleteCampaign(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _marketingFactory.DeleteCampaignAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("campaigns/segment-preview")]
    public async Task<ActionResult<SlnSegmentPreviewDto>> SegmentPreview([FromBody] string? segmentFilter, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var preview = await _marketingFactory.GetSegmentPreviewAsync(segmentFilter, customerId, GetBranchId() ?? branchId);
        return Ok(preview);
    }

    [HttpGet("campaigns/segment-presets")]
    public async Task<ActionResult<List<SlnSegmentPresetDto>>> GetSegmentPresets([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var presets = await _marketingFactory.GetSegmentPresetsAsync(customerId, GetBranchId() ?? branchId);
        return Ok(presets);
    }

    [HttpPost("campaigns/{id:int}/send")]
    public async Task<ActionResult> SendCampaign(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _marketingFactory.SendCampaignAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(error);
    }

    // ═══ Oto-Hatirlatma ═══

    [HttpGet("reminders")]
    public async Task<ActionResult<List<SlnAutoReminderDto>>> GetReminders([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var reminders = await _marketingFactory.GetRemindersAsync(customerId, GetBranchId() ?? branchId);
        return Ok(reminders);
    }

    [HttpPost("reminders")]
    public async Task<ActionResult<SlnAutoReminderDto>> CreateReminder([FromBody] SlnAutoReminderCreateDto dto, [FromQuery] int? branchId, [FromQuery] bool allBranches = false)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveMutationBranchTarget(branchId, allBranches);
        if (target.Error != null) return target.Error;

        var reminder = await _marketingFactory.CreateReminderAsync(dto, customerId, target.BranchId);
        return Ok(reminder);
    }

    [HttpPut("reminders/{id:int}")]
    public async Task<ActionResult> UpdateReminder(int id, [FromBody] SlnAutoReminderUpdateDto dto, [FromQuery] int? branchId, [FromQuery] bool allBranches = false)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveMutationBranchTarget(branchId, allBranches);
        if (target.Error != null) return target.Error;

        var (success, error) = await _marketingFactory.UpdateReminderAsync(id, dto, customerId, target.BranchId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("reminders/{id:int}")]
    public async Task<ActionResult> DeleteReminder(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _marketingFactory.DeleteReminderAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("reminders/{id:int}/toggle")]
    public async Task<ActionResult> ToggleReminder(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _marketingFactory.ToggleReminderAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(error);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }

    private (int? BranchId, ActionResult? Error) ResolveMutationBranchTarget(int? requestedBranchId, bool allBranches)
    {
        var claimBranchId = GetBranchId();
        if (claimBranchId.HasValue) return (claimBranchId.Value, null);
        if (allBranches) return (null, null);
        if (requestedBranchId.HasValue && requestedBranchId.Value > 0) return (requestedBranchId.Value, null);
        return (null, BadRequest(BranchTargetRequiredMessage));
    }
}
