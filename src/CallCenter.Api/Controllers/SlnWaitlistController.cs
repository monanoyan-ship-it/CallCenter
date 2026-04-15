using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-waitlist")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnWaitlist)]
public class SlnWaitlistController : ControllerBase
{
    private readonly ISlnWaitlistFactory _factory;

    public SlnWaitlistController(ISlnWaitlistFactory factory) => _factory = factory;

    [HttpGet]
    public async Task<ActionResult<List<SlnWaitlistEntryDto>>> GetEntries([FromQuery] DateTime? date, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        // JWT'de BranchId varsa kilit (personel) — yoksa query'den al (SalonOwner)
        var jwtBranch = User.FindFirst("BranchId")?.Value;
        var effectiveBranch = (jwtBranch != null && int.TryParse(jwtBranch, out var jb) && jb > 0) ? jb : branchId;
        return Ok(await _factory.GetEntriesAsync(customerId, date, effectiveBranch));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SlnWaitlistEntryDto>> GetEntry(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var entry = await _factory.GetEntryAsync(id, customerId);
        return entry != null ? Ok(entry) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<SlnWaitlistEntryDto>> CreateEntry([FromBody] SlnWaitlistEntryCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.CreateEntryAsync(dto, customerId));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateEntry(int id, [FromBody] SlnWaitlistEntryUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.UpdateEntryAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPut("{id}/status/{statusId}")]
    public async Task<ActionResult> UpdateStatus(int id, int statusId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.UpdateStatusAsync(id, statusId, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteEntry(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.DeleteEntryAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
