using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-winback")]
[Authorize]
public class SlnWinbackController : ControllerBase
{
    private readonly ISlnWinbackFactory _factory;

    public SlnWinbackController(ISlnWinbackFactory factory) => _factory = factory;

    [HttpGet]
    public async Task<ActionResult<List<SlnWinbackRuleDto>>> GetRules()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetRulesAsync(customerId));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SlnWinbackRuleDto>> GetRule(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var rule = await _factory.GetRuleAsync(id, customerId);
        return rule != null ? Ok(rule) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<SlnWinbackRuleDto>> CreateRule([FromBody] SlnWinbackRuleCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.CreateRuleAsync(dto, customerId));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateRule(int id, [FromBody] SlnWinbackRuleUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.UpdateRuleAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRule(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.DeleteRuleAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("{id}/toggle")]
    public async Task<ActionResult> ToggleRule(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.ToggleRuleAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
