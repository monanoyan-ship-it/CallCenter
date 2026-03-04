using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/portal")]
[Authorize(Roles = "Admin,CustomerUser")]
public class PortalController : AuditableControllerBase
{
    private readonly IPortalFactory _portalFactory;

    public PortalController(IAuditFactory auditFactory, IPortalFactory portalFactory) : base(auditFactory)
    {
        _portalFactory = portalFactory;
    }

    // HELPERS

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");

    private bool IsCustomerAdmin => User.FindFirstValue("IsCustomerAdmin") == "true";

    private int? ResolveCustomerId(int? queryCustomerId)
    {
        if (IsAdmin)
            return queryCustomerId;

        var claim = User.FindFirstValue("CustomerId");
        return claim != null ? int.Parse(claim) : null;
    }

    private bool HasPermission(int permTypeId)
    {
        if (IsAdmin || IsCustomerAdmin) return true;

        var perms = User.FindFirstValue("CustomerPermissions");
        if (string.IsNullOrEmpty(perms)) return false;

        return perms.Split(',').Any(p => int.TryParse(p.Trim(), out var id) && id == permTypeId);
    }

    // DASHBOARD

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] int? customerId)
    {
        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var result = await _portalFactory.GetDashboardAsync(cid.Value);
        return Ok(result);
    }

    // PERSONNEL

    [HttpGet("personnel")]
    public async Task<IActionResult> GetPersonnel([FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelView))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        return Ok(await _portalFactory.GetPersonnelAsync(cid.Value));
    }

    [HttpPost("personnel")]
    public async Task<IActionResult> CreatePersonnel([FromBody] PortalPersonnelCreateDto dto, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var (success, result) = await _portalFactory.CreatePersonnelAsync(cid.Value, dto, GetUserId());
        if (!success) return BadRequest(new { message = (string)result });

        await AuditCrudAsync("Create", "Personnel", result?.ToString(),
            $"Personel olusturuldu: '{dto.FullName}' ({dto.UserName})", customerId: cid);

        return Ok(result);
    }

    [HttpPut("personnel/{id}")]
    public async Task<IActionResult> UpdatePersonnel(int id, [FromBody] PortalPersonnelUpdateDto dto, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var (success, error) = await _portalFactory.UpdatePersonnelAsync(cid.Value, id, dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Update", "Personnel", id.ToString(),
            $"Personel guncellendi: ID={id}", customerId: cid);

        return NoContent();
    }

    [HttpDelete("personnel/{id}")]
    public async Task<IActionResult> DeactivatePersonnel(int id, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var (success, error) = await _portalFactory.DeactivatePersonnelAsync(cid.Value, id);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Deactivate", "Personnel", id.ToString(),
            $"Personel deaktif edildi: ID={id}", customerId: cid);

        return NoContent();
    }

    [HttpGet("personnel/{id}/permissions")]
    public async Task<IActionResult> GetPersonnelPermissions(int id, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelView))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        return Ok(await _portalFactory.GetPersonnelPermissionsAsync(cid.Value, id));
    }

    [HttpPost("personnel/{id}/permissions")]
    public async Task<IActionResult> SetPersonnelPermissions(int id, [FromBody] SetPersonnelPermissionsRequest request, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var (success, error) = await _portalFactory.SetPersonnelPermissionsAsync(cid.Value, id, request.PermissionTypeIds, request.ScopeId, GetUserId());
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("UpdatePermissions", "Personnel", id.ToString(),
            $"Personel yetkileri guncellendi: ID={id}, izinler=[{string.Join(",", request.PermissionTypeIds)}]",
            customerId: cid);

        return NoContent();
    }

    // MODULES

    [HttpGet("modules")]
    public async Task<IActionResult> GetModules([FromQuery] int? customerId)
    {
        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        return Ok(await _portalFactory.GetModulesAsync(cid.Value));
    }

    // SIP

    [HttpGet("sip")]
    public async Task<IActionResult> GetSipAccounts([FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.SipView))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        return Ok(await _portalFactory.GetSipAccountsAsync(cid.Value));
    }

    [HttpPost("sip")]
    public async Task<IActionResult> CreateSipAccount([FromBody] PortalSipCreateDto dto, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.SipManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var (success, id, error) = await _portalFactory.CreateSipAccountAsync(cid.Value, dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "SipAccount", id.ToString(),
            $"Portal SIP hesabi olusturuldu: '{dto.Name}' ({dto.Server}:{dto.Port})", customerId: cid);

        return Ok(new { id });
    }

    [HttpPut("sip/{id}")]
    public async Task<IActionResult> UpdateSipAccount(int id, [FromBody] PortalSipUpdateDto dto, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.SipManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var (success, error) = await _portalFactory.UpdateSipAccountAsync(cid.Value, id, dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Update", "SipAccount", id.ToString(),
            $"Portal SIP hesabi guncellendi: ID={id}", customerId: cid);

        return NoContent();
    }
}
