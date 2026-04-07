using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-memberships")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnMemberships)]
public class SlnMembershipController : ControllerBase
{
    private readonly ISlnMembershipFactory _factory;

    public SlnMembershipController(ISlnMembershipFactory factory) => _factory = factory;

    [HttpGet("plans")]
    public async Task<ActionResult<List<SlnMembershipPlanDto>>> GetPlans()
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        return Ok(await _factory.GetPlansAsync(cid));
    }

    [HttpPost("plans")]
    public async Task<ActionResult<SlnMembershipPlanDto>> CreatePlan([FromBody] SlnMembershipPlanCreateDto dto)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        return Ok(await _factory.CreatePlanAsync(dto, cid));
    }

    [HttpPut("plans/{id}")]
    public async Task<ActionResult> UpdatePlan(int id, [FromBody] SlnMembershipPlanCreateDto dto)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        var (s, e) = await _factory.UpdatePlanAsync(id, dto, cid);
        return s ? Ok() : BadRequest(e);
    }

    [HttpDelete("plans/{id}")]
    public async Task<ActionResult> DeletePlan(int id)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        var (s, e) = await _factory.DeletePlanAsync(id, cid);
        return s ? Ok() : BadRequest(e);
    }

    [HttpGet]
    public async Task<ActionResult<List<SlnClientMembershipDto>>> GetMemberships([FromQuery] int? clientId)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        return Ok(await _factory.GetMembershipsAsync(cid, clientId));
    }

    [HttpPost]
    public async Task<ActionResult<SlnClientMembershipDto>> CreateMembership([FromBody] SlnClientMembershipCreateDto dto)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        var (m, e) = await _factory.CreateMembershipAsync(dto, cid);
        return m != null ? Ok(m) : BadRequest(e);
    }

    [HttpPut("{id}/cancel")]
    public async Task<ActionResult> Cancel(int id)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        var (s, e) = await _factory.CancelMembershipAsync(id, cid);
        return s ? Ok() : BadRequest(e);
    }

    [HttpPut("{id}/freeze")]
    public async Task<ActionResult> Freeze(int id)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        var (s, e) = await _factory.FreezeMembershipAsync(id, cid);
        return s ? Ok() : BadRequest(e);
    }

    [HttpPut("{id}/reactivate")]
    public async Task<ActionResult> Reactivate(int id)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        var (s, e) = await _factory.ReactivateMembershipAsync(id, cid);
        return s ? Ok() : BadRequest(e);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
