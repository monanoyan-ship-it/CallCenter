using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Services;
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
    private readonly IOrganizationFactory _orgFactory;
    private readonly ICustomerFactory _customerFactory;
    private readonly GcsUploadService _gcs;

    public PortalController(IAuditFactory auditFactory, IPortalFactory portalFactory, IOrganizationFactory orgFactory, ICustomerFactory customerFactory, GcsUploadService gcs) : base(auditFactory)
    {
        _portalFactory = portalFactory;
        _orgFactory = orgFactory;
        _customerFactory = customerFactory;
        _gcs = gcs;
    }

    // ... (Helpers buraya gelecek)

    // ─── SETTINGS ───

    [HttpGet("settings")]
    public async Task<IActionResult> GetMySettings()
    {
        var cid = CurrentCustomerId;
        if (cid == null) return Unauthorized();

        var customer = await _customerFactory.GetRawByIdAsync(cid.Value);
        if (customer == null) return NotFound();

        return Ok(new CustomerSettingsDto
        {
            AutoRecordCalls = customer.AutoRecordCalls,
            IsCallbackManagementEnabled = customer.IsCallbackManagementEnabled
        });
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateMySettings([FromBody] UpdateCustomerSettingsRequest request)
    {
        if (!IsCustomerAdmin) return Forbid();

        var cid = CurrentCustomerId;
        if (cid == null) return Unauthorized();

        var (success, error) = await _customerFactory.UpdateSettingsAsync(cid.Value, request);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("UpdateSettings", "Customer", cid.ToString(), "Musteri genel ayarlari guncellendi.", customerId: cid);
        return Ok();
    }

    // HELPERS

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");

    private bool IsCustomerAdmin => User.FindFirstValue("IsCustomerAdmin") == "true";

    private int? GetCustomerPersonnelId()
    {
        var claim = User.FindFirstValue("CustomerPersonnelId");
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }

    private int? GetCustomerRoleId()
    {
        var claim = User.FindFirstValue("CustomerRoleId");
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }

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

        var result = await _portalFactory.GetDashboardAsync(cid.Value, GetCustomerPersonnelId(), GetCustomerRoleId());
        return Ok(result);
    }

    // LOOKUPS

    /// <summary>Salon rolleri (SalonRoles TypeItem listesi)</summary>
    [HttpGet("salon-roles")]
    public IActionResult GetSalonRoles()
    {
        var roles = SalonRoles.All
            .Where(r => r.Id != SalonRoles.Ids.SalonOwner) // Salon Sahibi atanamazm kayit sirasinda olusturulur
            .Select(r => new { r.Id, name = r.SystemName, displayName = r.Description, r.Icon }).ToList();
        return Ok(roles);
    }

    // PERSONNEL

    /// <summary>Kullanici adi musaitlik kontrolu</summary>
    [HttpGet("personnel/check-username")]
    public async Task<IActionResult> CheckUsername([FromQuery] string username, [FromQuery] int? excludeUserId)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Ok(new { available = false });

        var available = await _portalFactory.IsUsernameAvailableAsync(username.Trim(), excludeUserId);
        return Ok(new { available });
    }

    [HttpGet("personnel")]
    public async Task<IActionResult> GetPersonnel([FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelView))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        return Ok(await _portalFactory.GetPersonnelAsync(cid.Value, GetCustomerPersonnelId(), GetCustomerRoleId()));
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

        var (success, error) = await _portalFactory.UpdatePersonnelAsync(cid.Value, id, dto, IsAdmin);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Update", "Personnel", id.ToString(),
            $"Personel guncellendi: ID={id}", customerId: cid);

        return NoContent();
    }

    /// <summary>Personel profil fotografi yukle (max 3 MB, JPEG/PNG/WebP)</summary>
    [HttpPost("personnel/{id}/upload-photo")]
    [RequestSizeLimit(3_145_728)]
    public async Task<IActionResult> UploadPersonnelPhoto(int id, IFormFile file, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        if (file == null || file.Length == 0) return BadRequest("Dosya secilmedi.");
        if (file.Length > 3_145_728) return BadRequest("Dosya 3 MB'dan buyuk olamaz.");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest("Sadece JPEG, PNG ve WebP desteklenir.");

        var ext = file.ContentType switch { "image/png" => ".png", "image/webp" => ".webp", _ => ".jpg" };
        var path = $"personnel/{cid.Value}/{id}-{Guid.NewGuid():N}{ext}";

        using var stream = file.OpenReadStream();
        var (url, error) = await _gcs.UploadAsync(stream, path, file.ContentType);
        if (url == null) return BadRequest(error ?? "Yukleme hatasi.");

        var (ok, err) = await _portalFactory.UpdatePersonnelPhotoAsync(cid.Value, id, url);
        if (!ok) return BadRequest(err ?? "Foto guncellenemedi.");

        return Ok(new { url, path });
    }

    [HttpPatch("personnel/{id}/reports-to")]
    public async Task<IActionResult> SetReportsTo(int id, [FromBody] SetReportsToDto dto, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var (success, error) = await _portalFactory.SetReportsToAsync(cid.Value, id, dto.ReportsToPersonnelId);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("SetReportsTo", "Personnel", id.ToString(),
            $"Personel amir atamasi: ID={id}, AmirID={dto.ReportsToPersonnelId?.ToString() ?? "null"}", customerId: cid);

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

    [HttpPut("personnel/{id}/reactivate")]
    public async Task<IActionResult> ReactivatePersonnel(int id, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var (success, error) = await _portalFactory.ReactivatePersonnelAsync(cid.Value, id);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Reactivate", "Personnel", id.ToString(),
            $"Personel aktiflestirildi: ID={id}", customerId: cid);

        return NoContent();
    }

    [HttpPut("personnel/{id}/unlock")]
    public async Task<IActionResult> UnlockPersonnel(int id, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.PersonnelManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var (success, error) = await _portalFactory.UnlockPersonnelAsync(cid.Value, id);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Unlock", "Personnel", id.ToString(),
            $"Personel hesap kilidi kaldirildi: ID={id}", customerId: cid);

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

    [HttpDelete("sip/{id}")]
    public async Task<IActionResult> DeleteSipAccount(int id, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.SipManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var (success, error) = await _portalFactory.DeleteSipAccountAsync(cid.Value, id);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Delete", "SipAccount", id.ToString(),
            $"Portal SIP hesabi silindi: ID={id}", customerId: cid);

        return NoContent();
    }

    // ORGANIZATIONS

    [HttpGet("organizations/tree")]
    public async Task<IActionResult> GetOrganizationTree([FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.OrgView))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        return Ok(await _orgFactory.GetTreeAsync(cid.Value));
    }

    [HttpGet("organizations/parents")]
    public async Task<IActionResult> GetOrganizationParents([FromQuery] int? customerId, [FromQuery] int? excludeId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.OrgView))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        return Ok(await _orgFactory.GetPotentialParentsAsync(cid.Value, excludeId));
    }

    [HttpGet("organizations/{id}")]
    public async Task<IActionResult> GetOrganization(int id, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.OrgView))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var result = await _orgFactory.GetByIdAsync(cid.Value, id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("organizations")]
    public async Task<IActionResult> CreateOrganization([FromBody] OrgUnitCreateDto dto, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.OrgManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var (success, result) = await _orgFactory.CreateAsync(cid.Value, dto);
        if (!success) return BadRequest(new { error = (string)result });

        await AuditCrudAsync("Create", "OrganizationUnit", result?.ToString(),
            $"Portal organizasyon birimi olusturuldu: '{dto.Name}'", customerId: cid);

        return Ok(result);
    }

    [HttpPut("organizations/{id}")]
    public async Task<IActionResult> UpdateOrganization(int id, [FromBody] OrgUnitUpdateDto dto, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.OrgManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var (success, error) = await _orgFactory.UpdateAsync(cid.Value, id, dto);
        if (!success) return BadRequest(new { error });

        await AuditCrudAsync("Update", "OrganizationUnit", id.ToString(),
            $"Portal organizasyon birimi guncellendi: ID={id}", customerId: cid);

        return NoContent();
    }

    [HttpDelete("organizations/{id}")]
    public async Task<IActionResult> DeleteOrganization(int id, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.OrgManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var (success, error) = await _orgFactory.DeleteAsync(cid.Value, id);
        if (!success) return BadRequest(new { error });

        await AuditCrudAsync("Delete", "OrganizationUnit", id.ToString(),
            $"Portal organizasyon birimi silindi: ID={id}", customerId: cid);

        return NoContent();
    }

    // ─── PERSONEL ATAMA (ORGANİZASYON) ───

    [HttpGet("organizations/{id}/available-personnel")]
    public async Task<IActionResult> GetAvailablePersonnelForOrg(int id, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.OrgView))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var list = await _orgFactory.GetAvailablePersonnelAsync(cid.Value, id);
        return Ok(list);
    }

    [HttpPost("organizations/{id}/personnel")]
    public async Task<IActionResult> AssignPersonnelToOrg(int id, [FromBody] OrgPersonnelAssignDto dto, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.OrgManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var (success, error) = await _orgFactory.AssignPersonnelAsync(cid.Value, id, dto.PersonnelId);
        if (!success) return BadRequest(new { error });

        await AuditCrudAsync("AssignPersonnel", "OrganizationUnit", id.ToString(),
            $"Portal personel atandi: OrgID={id}, PersonelID={dto.PersonnelId}", customerId: cid);

        return Ok();
    }

    [HttpDelete("organizations/{id}/personnel/{personnelId}")]
    public async Task<IActionResult> RemovePersonnelFromOrg(int id, int personnelId, [FromQuery] int? customerId)
    {
        if (!HasPermission(CustomerPermissionTypes.Ids.OrgManage))
            return Forbid();

        var cid = ResolveCustomerId(customerId);
        if (cid == null) return BadRequest("CustomerId gerekli.");

        var (success, error) = await _orgFactory.RemovePersonnelAsync(cid.Value, id, personnelId);
        if (!success) return BadRequest(new { error });

        await AuditCrudAsync("RemovePersonnel", "OrganizationUnit", id.ToString(),
            $"Portal personel cikarildi: OrgID={id}, PersonelID={personnelId}", customerId: cid);

        return NoContent();
    }
}
