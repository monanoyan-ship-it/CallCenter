using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-marketing")]
[Authorize]
public class SlnMarketingController : ControllerBase
{
    private readonly ISlnMarketingFactory _marketingFactory;

    public SlnMarketingController(ISlnMarketingFactory marketingFactory) => _marketingFactory = marketingFactory;

    // ═══ Kampanya ═══

    [HttpGet("campaigns")]
    public async Task<ActionResult<List<SlnCampaignDto>>> GetCampaigns()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var campaigns = await _marketingFactory.GetCampaignsAsync(customerId);
        return Ok(campaigns);
    }

    [HttpGet("campaigns/{id}")]
    public async Task<ActionResult<SlnCampaignDto>> GetCampaign(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var campaign = await _marketingFactory.GetCampaignAsync(id, customerId);
        return campaign != null ? Ok(campaign) : NotFound();
    }

    [HttpPost("campaigns")]
    public async Task<ActionResult<SlnCampaignDto>> CreateCampaign([FromBody] SlnCampaignCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var campaign = await _marketingFactory.CreateCampaignAsync(dto, customerId);
        return Ok(campaign);
    }

    [HttpPut("campaigns/{id}")]
    public async Task<ActionResult> UpdateCampaign(int id, [FromBody] SlnCampaignUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _marketingFactory.UpdateCampaignAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("campaigns/{id}")]
    public async Task<ActionResult> DeleteCampaign(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _marketingFactory.DeleteCampaignAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("campaigns/segment-preview")]
    public async Task<ActionResult<SlnSegmentPreviewDto>> SegmentPreview([FromBody] string? segmentFilter)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var preview = await _marketingFactory.GetSegmentPreviewAsync(segmentFilter, customerId);
        return Ok(preview);
    }

    [HttpPost("campaigns/{id}/send")]
    public async Task<ActionResult> SendCampaign(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _marketingFactory.SendCampaignAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    // ═══ Oto-Hatirlatma ═══

    [HttpGet("reminders")]
    public async Task<ActionResult<List<SlnAutoReminderDto>>> GetReminders()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var reminders = await _marketingFactory.GetRemindersAsync(customerId);
        return Ok(reminders);
    }

    [HttpPost("reminders")]
    public async Task<ActionResult<SlnAutoReminderDto>> CreateReminder([FromBody] SlnAutoReminderCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var reminder = await _marketingFactory.CreateReminderAsync(dto, customerId);
        return Ok(reminder);
    }

    [HttpPut("reminders/{id}")]
    public async Task<ActionResult> UpdateReminder(int id, [FromBody] SlnAutoReminderUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _marketingFactory.UpdateReminderAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("reminders/{id}")]
    public async Task<ActionResult> DeleteReminder(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _marketingFactory.DeleteReminderAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("reminders/{id}/toggle")]
    public async Task<ActionResult> ToggleReminder(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _marketingFactory.ToggleReminderAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
