using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-email-campaigns")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnEmailCampaigns)]
public class SlnEmailCampaignController : ControllerBase
{
    private readonly ISlnEmailCampaignFactory _factory;

    public SlnEmailCampaignController(ISlnEmailCampaignFactory factory) => _factory = factory;

    [HttpGet]
    public async Task<ActionResult<List<SlnEmailCampaignDto>>> GetCampaigns()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetCampaignsAsync(customerId));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SlnEmailCampaignDto>> GetCampaign(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var campaign = await _factory.GetCampaignAsync(id, customerId);
        return campaign != null ? Ok(campaign) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<SlnEmailCampaignDto>> CreateCampaign([FromBody] SlnEmailCampaignCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.CreateCampaignAsync(dto, customerId));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateCampaign(int id, [FromBody] SlnEmailCampaignUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.UpdateCampaignAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteCampaign(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.DeleteCampaignAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("segment-preview")]
    public async Task<ActionResult<SlnSegmentPreviewDto>> SegmentPreview([FromBody] string? segmentFilter)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetSegmentPreviewAsync(segmentFilter, customerId));
    }

    [HttpGet("segment-presets")]
    public async Task<ActionResult<List<SlnSegmentPresetDto>>> GetSegmentPresets()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetSegmentPresetsAsync(customerId));
    }

    [HttpPost("{id:int}/send")]
    public async Task<ActionResult> SendCampaign(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.SendCampaignAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
