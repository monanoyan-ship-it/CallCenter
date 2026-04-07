using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-noshow-policy")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnNoShowPolicy)]
public class SlnNoShowPolicyController : ControllerBase
{
    private readonly ISlnNoShowPolicyFactory _factory;

    public SlnNoShowPolicyController(ISlnNoShowPolicyFactory factory) => _factory = factory;

    [HttpGet]
    public async Task<ActionResult> GetPolicy()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var policy = await _factory.GetPolicyAsync(customerId);
        return Ok(policy ?? new SlnNoShowPolicyDto
        {
            FreeCancellationHours = 24,
            BlacklistThreshold = 3,
            IsActive = true
        });
    }

    [HttpPost]
    public async Task<ActionResult<SlnNoShowPolicyDto>> SavePolicy([FromBody] SlnNoShowPolicyUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.SavePolicyAsync(dto, customerId));
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
