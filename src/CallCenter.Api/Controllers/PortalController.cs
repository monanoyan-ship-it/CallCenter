using System.Security.Claims;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/portal")]
[Authorize(Roles = "Admin,CustomerUser")]
public class PortalController : ControllerBase
{
    private readonly ServiceFactory _factory;

    public PortalController(ServiceFactory factory)
    {
        _factory = factory;
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");

    /// <summary>Musteri admin'i mi? (kendi musterisi icin tum izinler)</summary>
    private bool IsCustomerAdmin => User.FindFirstValue("IsCustomerAdmin") == "true";

    /// <summary>
    /// Admin ise ?customerId parametresinden, CustomerUser ise JWT claim'den customerId alir.
    /// </summary>
    private int? ResolveCustomerId(int? queryCustomerId)
    {
        if (IsAdmin)
            return queryCustomerId;

        var claim = User.FindFirstValue("CustomerId");
        return claim != null ? int.Parse(claim) : null;
    }

    /// <summary>
    /// System Admin ve CustomerAdmin muaf, normal CustomerUser icin JWT claim kontrolu.
    /// </summary>
    private bool HasPermission(int permTypeId)
    {
        if (IsAdmin || IsCustomerAdmin) return true;

        var perms = User.FindFirstValue("CustomerPermissions");
        if (string.IsNullOrEmpty(perms)) return false;

        return perms.Split(',').Any(p => int.TryParse(p.Trim(), out var id) && id == permTypeId);
    }

    // ═══════════════════════════════════════════════════════════════
    // DASHBOARD
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] int? customerId)
    {
        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        var result = await service.GetDashboardAsync(cid.Value);
        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // USERTYPES
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("usertypes")]
    public async Task<IActionResult> GetUserTypes([FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.UserTypeView))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        return Ok(await service.GetUserTypesAsync(cid.Value));
    }

    [HttpPost("usertypes")]
    public async Task<IActionResult> CreateUserType([FromBody] UserTypeCreateDto dto, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.UserTypeManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        var (success, result) = await service.CreateUserTypeAsync(cid.Value, dto);
        return success ? Ok(result) : BadRequest(new { message = (string)result });
    }

    [HttpPut("usertypes/{id}")]
    public async Task<IActionResult> UpdateUserType(int id, [FromBody] UserTypeUpdateDto dto, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.UserTypeManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        var (success, error) = await service.UpdateUserTypeAsync(cid.Value, id, dto);
        return success ? NoContent() : BadRequest(new { message = error });
    }

    [HttpDelete("usertypes/{id}")]
    public async Task<IActionResult> DeactivateUserType(int id, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.UserTypeManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        var (success, error) = await service.DeactivateUserTypeAsync(cid.Value, id);
        return success ? NoContent() : BadRequest(new { message = error });
    }

    [HttpGet("usertypes/{id}/permissions")]
    public async Task<IActionResult> GetUserTypePermissions(int id, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.UserTypeView))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        return Ok(await service.GetUserTypePermissionsAsync(cid.Value, id));
    }

    [HttpPost("usertypes/{id}/permissions")]
    public async Task<IActionResult> SetUserTypePermissions(int id, [FromBody] SetUserTypePermissionsRequest request, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.UserTypeManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        var (success, error) = await service.SetUserTypePermissionsAsync(cid.Value, id, request.PermissionTypeIds);
        return success ? NoContent() : BadRequest(new { message = error });
    }

    // ═══════════════════════════════════════════════════════════════
    // PERSONNEL
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("personnel")]
    public async Task<IActionResult> GetPersonnel([FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelView))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        return Ok(await service.GetPersonnelAsync(cid.Value));
    }

    [HttpPost("personnel")]
    public async Task<IActionResult> CreatePersonnel([FromBody] PortalPersonnelCreateDto dto, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        var (success, result) = await service.CreatePersonnelAsync(cid.Value, dto, GetUserId());
        return success ? Ok(result) : BadRequest(new { message = (string)result });
    }

    [HttpPut("personnel/{id}")]
    public async Task<IActionResult> UpdatePersonnel(int id, [FromBody] PortalPersonnelUpdateDto dto, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        var (success, error) = await service.UpdatePersonnelAsync(cid.Value, id, dto);
        return success ? NoContent() : BadRequest(new { message = error });
    }

    [HttpDelete("personnel/{id}")]
    public async Task<IActionResult> DeactivatePersonnel(int id, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        var (success, error) = await service.DeactivatePersonnelAsync(cid.Value, id);
        return success ? NoContent() : BadRequest(new { message = error });
    }

    [HttpGet("personnel/{id}/permissions")]
    public async Task<IActionResult> GetPersonnelPermissions(int id, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelView))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        return Ok(await service.GetPersonnelPermissionsAsync(cid.Value, id));
    }

    [HttpPost("personnel/{id}/permissions")]
    public async Task<IActionResult> SetPersonnelPermissions(int id, [FromBody] SetPersonnelPermissionsRequest request, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        var (success, error) = await service.SetPersonnelPermissionsAsync(cid.Value, id, request.PermissionTypeIds, request.ScopeId, GetUserId());
        return success ? NoContent() : BadRequest(new { message = error });
    }

    // ═══════════════════════════════════════════════════════════════
    // MODULES
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("modules")]
    public async Task<IActionResult> GetModules([FromQuery] int? customerId)
    {
        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        return Ok(await service.GetModulesAsync(cid.Value));
    }

    // ═══════════════════════════════════════════════════════════════
    // SIP
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("sip")]
    public async Task<IActionResult> GetSipAccounts([FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.SipView))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        return Ok(await service.GetSipAccountsAsync(cid.Value));
    }

    [HttpPut("sip/{id}")]
    public async Task<IActionResult> UpdateSipAccount(int id, [FromBody] PortalSipUpdateDto dto, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.SipManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var service = _factory.CreatePortalService();
        var (success, error) = await service.UpdateSipAccountAsync(cid.Value, id, dto);
        return success ? NoContent() : BadRequest(new { message = error });
    }
}
