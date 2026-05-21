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
    public async Task<ActionResult<List<SlnWaitlistEntryDto>>> GetEntries([FromQuery] DateTime? date, [FromQuery] int? branchId, [FromQuery] string? scope, [FromQuery] string? search)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var normalizedScope = SlnWaitlistStatuses.NormalizeScope(scope);
        if (normalizedScope == null) return BadRequest("Gecersiz bekleme listesi kapsami");
        // JWT'de BranchId varsa kilit (personel) — yoksa query'den al (SalonOwner)
        var effectiveBranch = GetBranchScopeId() ?? branchId;
        return Ok(await _factory.GetEntriesAsync(customerId, date, effectiveBranch, normalizedScope, search));
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
    public async Task<ActionResult<SlnWaitlistEntryDto>> CreateEntry([FromBody] SlnWaitlistEntryCreateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        dto.BranchId ??= branchId;
        var (success, error, entry) = await _factory.CreateEntryAsync(dto, customerId, GetBranchScopeId());
        return success ? Ok(entry) : BadRequest(error);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateEntry(int id, [FromBody] SlnWaitlistEntryUpdateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        dto.BranchId ??= branchId;
        var (success, error) = await _factory.UpdateEntryAsync(id, dto, customerId, GetBranchScopeId());
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

    /// <summary>BranchId NULL olan waitlist kayitlarini personelin/merkez subesine bagla.</summary>
    [HttpPost("normalize-branches")]
    public async Task<ActionResult> NormalizeBranches()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.NormalizeBranchesAsync(customerId));
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return int.TryParse(claim, out var id) && id > 0 ? id : null;
    }

    private int GetCustomerRoleId()
    {
        var claim = User.FindFirst("CustomerRoleId")?.Value;
        return int.TryParse(claim, out var roleId) ? roleId : SalonRoles.Ids.SalonOwner;
    }

    private int? GetBranchScopeId()
        => GetCustomerRoleId() == SalonRoles.Ids.SalonOwner ? null : GetBranchId();
}
