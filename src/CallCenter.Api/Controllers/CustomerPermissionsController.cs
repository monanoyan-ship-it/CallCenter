using System.Security.Claims;
using CallCenter.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/customers/{customerId}")]
[Authorize(Roles = "Admin,Supervisor")]
public class CustomerPermissionsController : ControllerBase
{
    private readonly ServiceFactory _factory;

    public CustomerPermissionsController(ServiceFactory factory)
    {
        _factory = factory;
    }

    // ═══════════════════════════════════════════════════════════
    // PORTAL MODÜL YÖNETİMİ (Müşteriye modül aç/kapa)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Müşterinin portal modüllerini getir</summary>
    [HttpGet("modules")]
    public async Task<IActionResult> GetCustomerModules(int customerId)
    {
        var svc = _factory.CreateCustomerService();
        var result = await svc.GetCustomerModulesAsync(customerId);
        if (result == null) return NotFound("Müşteri bulunamadı.");
        return Ok(result);
    }

    /// <summary>Müşteriye modül aç (toplu)</summary>
    [HttpPost("modules")]
    public async Task<IActionResult> AssignModules(int customerId, [FromBody] Shared.DTOs.AssignModulesRequest request)
    {
        var svc = _factory.CreateCustomerService();
        var (success, error) = await svc.AssignModulesAsync(customerId, request);
        if (!success) return NotFound(error);
        return Ok();
    }

    /// <summary>Müşteriden modül kapat</summary>
    [HttpDelete("modules/{moduleId}")]
    public async Task<IActionResult> DeactivateModule(int customerId, int moduleId)
    {
        var svc = _factory.CreateCustomerService();
        var (success, error) = await svc.DeactivateModuleAsync(customerId, moduleId);
        if (!success) return NotFound(error);
        return Ok();
    }

    // ═══════════════════════════════════════════════════════════
    // YETKİ TİPLERİ (Statik katalog — UI'da checkbox listesi)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Müşterinin açık modüllerindeki yetki tiplerini listele</summary>
    [HttpGet("permissions/types")]
    public async Task<IActionResult> GetAvailablePermissionTypes(int customerId)
    {
        var svc = _factory.CreateCustomerService();
        return Ok(await svc.GetAvailablePermissionTypesAsync(customerId));
    }

    // ═══════════════════════════════════════════════════════════
    // PERSONEL YETKİ YÖNETİMİ
    // ═══════════════════════════════════════════════════════════

    /// <summary>Personelin mevcut yetkilerini getir</summary>
    [HttpGet("personnel/{personnelId}/permissions")]
    public async Task<IActionResult> GetPersonnelPermissions(int customerId, int personnelId)
    {
        var svc = _factory.CreateCustomerService();
        var result = await svc.GetPersonnelPermissionsAsync(customerId, personnelId);
        if (result == null) return NotFound("Personel bulunamadı.");
        return Ok(result);
    }

    /// <summary>Personele toplu yetki ata</summary>
    [HttpPost("personnel/{personnelId}/permissions")]
    public async Task<IActionResult> AssignPermissions(int customerId, int personnelId, [FromBody] Shared.DTOs.AssignPermissionsRequest request)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var svc = _factory.CreateCustomerService();
        var (success, result, error) = await svc.AssignPermissionsAsync(customerId, personnelId, request, currentUserId);
        if (!success) return NotFound(error);
        return Ok(result);
    }

    /// <summary>Tek bir yetkiyi güncelle</summary>
    [HttpPut("personnel/{personnelId}/permissions/{id}")]
    public async Task<IActionResult> UpdatePermission(int customerId, int personnelId, int id, [FromBody] Shared.DTOs.UpdatePermissionRequest request)
    {
        var svc = _factory.CreateCustomerService();
        var (success, error) = await svc.UpdatePermissionAsync(customerId, personnelId, id, request);
        if (!success)
        {
            if (error == "Yetki bulunamadı.") return NotFound(error);
            return BadRequest(error);
        }
        return Ok();
    }

    /// <summary>Yetkiyi kaldır</summary>
    [HttpDelete("personnel/{personnelId}/permissions/{id}")]
    public async Task<IActionResult> RemovePermission(int customerId, int personnelId, int id)
    {
        var svc = _factory.CreateCustomerService();
        var (success, error) = await svc.RemovePermissionAsync(customerId, personnelId, id);
        if (!success) return NotFound(error);
        return Ok();
    }
}
