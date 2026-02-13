using System.Security.Claims;
using CallCenter.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/customers/{customerId}")]
[Authorize(Roles = "Admin,Supervisor")]
public class CustomerPermissionsController : AuditableControllerBase
{
    public CustomerPermissionsController(ServiceFactory factory) : base(factory) { }

    // ═══════════════════════════════════════════════════════════
    // PORTAL MODUL YONETIMI (Musteriye modul ac/kapa)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Musterinin portal modullerini getir</summary>
    [HttpGet("modules")]
    public async Task<IActionResult> GetCustomerModules(int customerId)
    {
        var svc = Factory.CreateCustomerService();
        var result = await svc.GetCustomerModulesAsync(customerId);
        if (result == null) return NotFound("Musteri bulunamadi.");
        return Ok(result);
    }

    /// <summary>Musteriye modul ac (toplu)</summary>
    [HttpPost("modules")]
    public async Task<IActionResult> AssignModules(int customerId, [FromBody] Shared.DTOs.AssignModulesRequest request)
    {
        var svc = Factory.CreateCustomerService();
        var (success, error) = await svc.AssignModulesAsync(customerId, request);
        if (!success) return NotFound(error);

        await AuditCrudAsync("AssignModules", "Customer", customerId.ToString(),
            $"Musteriye moduller atandi: [{string.Join(",", request.ModuleIds)}]", customerId: customerId);

        return Ok();
    }

    /// <summary>Musteriden modul kapat</summary>
    [HttpDelete("modules/{moduleId}")]
    public async Task<IActionResult> DeactivateModule(int customerId, int moduleId)
    {
        var svc = Factory.CreateCustomerService();
        var (success, error) = await svc.DeactivateModuleAsync(customerId, moduleId);
        if (!success) return NotFound(error);

        await AuditCrudAsync("DeactivateModule", "Customer", customerId.ToString(),
            $"Musteri modulu kapatildi: moduleId={moduleId}", customerId: customerId);

        return Ok();
    }

    // ═══════════════════════════════════════════════════════════
    // YETKI TIPLERI (Statik katalog — UI'da checkbox listesi)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Musterinin acik modullerindeki yetki tiplerini listele</summary>
    [HttpGet("permissions/types")]
    public async Task<IActionResult> GetAvailablePermissionTypes(int customerId)
    {
        var svc = Factory.CreateCustomerService();
        return Ok(await svc.GetAvailablePermissionTypesAsync(customerId));
    }

    // ═══════════════════════════════════════════════════════════
    // PERSONEL YETKI YONETIMI
    // ═══════════════════════════════════════════════════════════

    /// <summary>Personelin mevcut yetkilerini getir</summary>
    [HttpGet("personnel/{personnelId}/permissions")]
    public async Task<IActionResult> GetPersonnelPermissions(int customerId, int personnelId)
    {
        var svc = Factory.CreateCustomerService();
        var result = await svc.GetPersonnelPermissionsAsync(customerId, personnelId);
        if (result == null) return NotFound("Personel bulunamadi.");
        return Ok(result);
    }

    /// <summary>Personele toplu yetki ata</summary>
    [HttpPost("personnel/{personnelId}/permissions")]
    public async Task<IActionResult> AssignPermissions(int customerId, int personnelId, [FromBody] Shared.DTOs.AssignPermissionsRequest request)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var svc = Factory.CreateCustomerService();
        var (success, result, error) = await svc.AssignPermissionsAsync(customerId, personnelId, request, currentUserId);
        if (!success) return NotFound(error);

        await AuditCrudAsync("AssignPermissions", "Personnel", personnelId.ToString(),
            $"Personel ID={personnelId}'e yetkiler atandi: [{string.Join(",", request.PermissionTypeIds)}]",
            customerId: customerId);

        return Ok(result);
    }

    /// <summary>Tek bir yetkiyi guncelle</summary>
    [HttpPut("personnel/{personnelId}/permissions/{id}")]
    public async Task<IActionResult> UpdatePermission(int customerId, int personnelId, int id, [FromBody] Shared.DTOs.UpdatePermissionRequest request)
    {
        var svc = Factory.CreateCustomerService();
        var (success, error) = await svc.UpdatePermissionAsync(customerId, personnelId, id, request);
        if (!success)
        {
            if (error == "Yetki bulunamadi.") return NotFound(error);
            return BadRequest(error);
        }

        await AuditCrudAsync("UpdatePermission", "Permission", id.ToString(),
            $"Personel ID={personnelId} yetkisi guncellendi: permissionId={id}",
            customerId: customerId);

        return Ok();
    }

    /// <summary>Yetkiyi kaldir</summary>
    [HttpDelete("personnel/{personnelId}/permissions/{id}")]
    public async Task<IActionResult> RemovePermission(int customerId, int personnelId, int id)
    {
        var svc = Factory.CreateCustomerService();
        var (success, error) = await svc.RemovePermissionAsync(customerId, personnelId, id);
        if (!success) return NotFound(error);

        await AuditCrudAsync("RemovePermission", "Permission", id.ToString(),
            $"Personel ID={personnelId} yetkisi kaldirildi: permissionId={id}",
            customerId: customerId);

        return Ok();
    }
}
