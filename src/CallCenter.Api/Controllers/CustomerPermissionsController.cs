using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/customers/{customerId}")]
[Authorize(Roles = "Admin,Supervisor")]
public class CustomerPermissionsController : AuditableControllerBase
{
    private readonly ICustomerFactory _customerFactory;

    public CustomerPermissionsController(IAuditFactory auditFactory, ICustomerFactory customerFactory) : base(auditFactory)
    {
        _customerFactory = customerFactory;
    }

    // PORTAL MODUL YONETIMI

    [HttpGet("modules")]
    public async Task<IActionResult> GetCustomerModules(int customerId)
    {
        var result = await _customerFactory.GetCustomerModulesAsync(customerId);
        if (result == null) return NotFound("Musteri bulunamadi.");
        return Ok(result);
    }

    [HttpPost("modules")]
    public async Task<IActionResult> AssignModules(int customerId, [FromBody] Shared.DTOs.AssignModulesRequest request)
    {
        var (success, error) = await _customerFactory.AssignModulesAsync(customerId, request);
        if (!success) return NotFound(error);

        await AuditCrudAsync("AssignModules", "Customer", customerId.ToString(),
            $"Musteriye moduller atandi: [{string.Join(",", request.ModuleIds)}]", customerId: customerId);

        return Ok();
    }

    [HttpDelete("modules/{moduleId}")]
    public async Task<IActionResult> DeactivateModule(int customerId, int moduleId)
    {
        var (success, error) = await _customerFactory.DeactivateModuleAsync(customerId, moduleId);
        if (!success) return NotFound(error);

        await AuditCrudAsync("DeactivateModule", "Customer", customerId.ToString(),
            $"Musteri modulu kapatildi: moduleId={moduleId}", customerId: customerId);

        return Ok();
    }

    // YETKI TIPLERI

    [HttpGet("permissions/types")]
    public async Task<IActionResult> GetAvailablePermissionTypes(int customerId)
    {
        return Ok(await _customerFactory.GetAvailablePermissionTypesAsync(customerId));
    }

    // PERSONEL YETKI YONETIMI

    [HttpGet("personnel/{personnelId}/permissions")]
    public async Task<IActionResult> GetPersonnelPermissions(int customerId, int personnelId)
    {
        var result = await _customerFactory.GetPersonnelPermissionsAsync(customerId, personnelId);
        if (result == null) return NotFound("Personel bulunamadi.");
        return Ok(result);
    }

    [HttpPost("personnel/{personnelId}/permissions")]
    public async Task<IActionResult> AssignPermissions(int customerId, int personnelId, [FromBody] Shared.DTOs.AssignPermissionsRequest request)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, result, error) = await _customerFactory.AssignPermissionsAsync(customerId, personnelId, request, currentUserId);
        if (!success) return NotFound(error);

        await AuditCrudAsync("AssignPermissions", "Personnel", personnelId.ToString(),
            $"Personel ID={personnelId}'e yetkiler atandi: [{string.Join(",", request.PermissionTypeIds)}]",
            customerId: customerId);

        return Ok(result);
    }

    [HttpPut("personnel/{personnelId}/permissions/{id}")]
    public async Task<IActionResult> UpdatePermission(int customerId, int personnelId, int id, [FromBody] Shared.DTOs.UpdatePermissionRequest request)
    {
        var (success, error) = await _customerFactory.UpdatePermissionAsync(customerId, personnelId, id, request);
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

    [HttpDelete("personnel/{personnelId}/permissions/{id}")]
    public async Task<IActionResult> RemovePermission(int customerId, int personnelId, int id)
    {
        var (success, error) = await _customerFactory.RemovePermissionAsync(customerId, personnelId, id);
        if (!success) return NotFound(error);

        await AuditCrudAsync("RemovePermission", "Permission", id.ToString(),
            $"Personel ID={personnelId} yetkisi kaldirildi: permissionId={id}",
            customerId: customerId);

        return Ok();
    }
}
