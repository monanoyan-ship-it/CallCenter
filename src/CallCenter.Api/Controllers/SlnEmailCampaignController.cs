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

    [HttpGet("{id}")]
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

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateCampaign(int id, [FromBody] SlnEmailCampaignUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.UpdateCampaignAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCampaign(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.DeleteCampaignAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
