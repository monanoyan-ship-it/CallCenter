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
        var access = ResolveWaitlistBranchAccess();
        if (!access.IsAllowed) return access.ErrorResult!;
        // JWT'de BranchId varsa kilit (personel) — yoksa query'den al (SalonOwner)
        var effectiveBranch = access.BranchScopeId ?? branchId;
        return Ok(await _factory.GetEntriesAsync(customerId, date, effectiveBranch, normalizedScope, search));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SlnWaitlistEntryDto>> GetEntry(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var access = ResolveWaitlistBranchAccess();
        if (!access.IsAllowed) return access.ErrorResult!;
        var entry = await _factory.GetEntryAsync(id, customerId, access.BranchScopeId);
        return entry != null ? Ok(entry) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<SlnWaitlistEntryDto>> CreateEntry([FromBody] SlnWaitlistEntryCreateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        dto.BranchId ??= branchId;
        var access = ResolveWaitlistBranchAccess();
        if (!access.IsAllowed) return access.ErrorResult!;
        var (success, error, entry) = await _factory.CreateEntryAsync(dto, customerId, access.BranchScopeId);
        return success ? Ok(entry) : BadRequest(error);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateEntry(int id, [FromBody] SlnWaitlistEntryUpdateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        dto.BranchId ??= branchId;
        var access = ResolveWaitlistBranchAccess();
        if (!access.IsAllowed) return access.ErrorResult!;
        var (success, error) = await _factory.UpdateEntryAsync(id, dto, customerId, access.BranchScopeId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPut("{id}/status/{statusId}")]
    public async Task<ActionResult> UpdateStatus(int id, int statusId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var access = ResolveWaitlistBranchAccess();
        if (!access.IsAllowed) return access.ErrorResult!;
        var (success, error) = await _factory.UpdateStatusAsync(id, statusId, customerId, access.BranchScopeId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("{id}/convert")]
    [RequireModule(SalonPortalModules.Ids.SlnAppointments)]
    public async Task<ActionResult<SlnWaitlistConversionDto>> ConvertToAppointment(int id, [FromBody] SlnWaitlistConvertToAppointmentDto dto, [FromQuery] int? branchId)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var access = ResolveWaitlistBranchAccess();
        if (!access.IsAllowed) return access.ErrorResult!;
        dto.BranchId ??= branchId;
        var (success, error, result) = await _factory.ConvertToAppointmentAsync(id, dto, userId, customerId, access.BranchScopeId);
        return success ? Ok(result) : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteEntry(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var access = ResolveWaitlistBranchAccess();
        if (!access.IsAllowed) return access.ErrorResult!;
        var (success, error) = await _factory.DeleteEntryAsync(id, customerId, access.BranchScopeId);
        return success ? Ok() : BadRequest(error);
    }

    /// <summary>BranchId NULL olan waitlist kayitlarini personelin/merkez subesine bagla.</summary>
    [HttpPost("normalize-branches")]
    [RequireSalonOwner]
    public async Task<ActionResult> NormalizeBranches()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.NormalizeBranchesAsync(customerId));
    }

    private int GetUserId()
        => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return int.TryParse(claim, out var id) && id > 0 ? id : null;
    }

    private bool IsSalonOwner()
    {
        if (User.IsInRole("Admin")) return true;
        var claim = User.FindFirst("CustomerRoleId")?.Value;
        return int.TryParse(claim, out var roleId) && roleId == SalonRoles.Ids.SalonOwner;
    }

    private WaitlistBranchAccess ResolveWaitlistBranchAccess()
    {
        if (IsSalonOwner()) return new(true, null, null);
        var branchId = GetBranchId();
        return branchId.HasValue ? new(true, branchId.Value, null) : new(false, null, Forbid());
    }

    private readonly record struct WaitlistBranchAccess(bool IsAllowed, int? BranchScopeId, ActionResult? ErrorResult);
}
