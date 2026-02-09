using System.Security.Claims;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/customers/{customerId}")]
[Authorize(Roles = "Admin,Supervisor")]
public class CustomerPermissionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomerPermissionsController(AppDbContext db)
    {
        _db = db;
    }

    // ═══════════════════════════════════════════════════════════
    // PORTAL MODÜL YÖNETİMİ (Müşteriye modül aç/kapa)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Müşterinin portal modüllerini getir</summary>
    [HttpGet("modules")]
    public async Task<IActionResult> GetCustomerModules(int customerId)
    {
        var customer = await _db.Customers
            .Include(c => c.PortalModules)
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null) return NotFound("Müşteri bulunamadı.");

        // Tüm modülleri döndür, aktif olanları işaretle
        var result = PortalModules.All.Select(m =>
        {
            var assigned = customer.PortalModules.FirstOrDefault(pm => pm.ModuleId == m.Id);
            return new PortalModuleDto
            {
                Id = m.Id,
                SystemName = m.SystemName,
                Description = m.Description,
                Icon = m.Icon,
                IsActive = assigned?.IsActive ?? false,
                Permissions = CustomerPermissionTypes.GetByModule(m.Id).Select(p => new PermissionTypeDto
                {
                    Id = p.Id,
                    SystemName = p.SystemName,
                    Description = p.Description,
                    Icon = p.Icon,
                    ModuleId = m.Id,
                    ModuleName = m.SystemName
                }).ToList()
            };
        }).ToList();

        return Ok(result);
    }

    /// <summary>Müşteriye modül aç (toplu)</summary>
    [HttpPost("modules")]
    public async Task<IActionResult> AssignModules(int customerId, [FromBody] AssignModulesRequest request)
    {
        var customer = await _db.Customers
            .Include(c => c.PortalModules)
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null) return NotFound("Müşteri bulunamadı.");

        foreach (var moduleId in request.ModuleIds)
        {
            // Modül geçerli mi?
            if (PortalModules.GetById(moduleId) == null) continue;

            // Zaten atanmış mı?
            var existing = customer.PortalModules.FirstOrDefault(m => m.ModuleId == moduleId);
            if (existing != null)
            {
                // Varsa aktif et
                existing.IsActive = true;
                existing.DeactivatedAt = null;
            }
            else
            {
                // Yoksa ekle
                customer.PortalModules.Add(new CustomerPortalModule
                {
                    CustomerId = customerId,
                    ModuleId = moduleId,
                    IsActive = true,
                    Notes = request.Notes
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>Müşteriden modül kapat</summary>
    [HttpDelete("modules/{moduleId}")]
    public async Task<IActionResult> DeactivateModule(int customerId, int moduleId)
    {
        var module = await _db.CustomerPortalModules
            .FirstOrDefaultAsync(m => m.CustomerId == customerId && m.ModuleId == moduleId);

        if (module == null) return NotFound("Modül ataması bulunamadı.");

        module.IsActive = false;
        module.DeactivatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok();
    }

    // ═══════════════════════════════════════════════════════════
    // YETKİ TİPLERİ (Statik katalog — UI'da checkbox listesi)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Müşterinin açık modüllerindeki yetki tiplerini listele</summary>
    [HttpGet("permissions/types")]
    public async Task<IActionResult> GetAvailablePermissionTypes(int customerId)
    {
        // Müşterinin açık modüllerini bul
        var activeModuleIds = await _db.CustomerPortalModules
            .Where(m => m.CustomerId == customerId && m.IsActive)
            .Select(m => m.ModuleId)
            .ToListAsync();

        // Sadece açık modüllerdeki yetki tiplerini döndür
        var result = activeModuleIds
            .SelectMany(moduleId =>
            {
                var module = PortalModules.GetById(moduleId);
                return CustomerPermissionTypes.GetByModule(moduleId).Select(p => new PermissionTypeDto
                {
                    Id = p.Id,
                    SystemName = p.SystemName,
                    Description = p.Description,
                    Icon = p.Icon,
                    ModuleId = moduleId,
                    ModuleName = module?.SystemName
                });
            })
            .ToList();

        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════
    // PERSONEL YETKİ YÖNETİMİ
    // ═══════════════════════════════════════════════════════════

    /// <summary>Personelin mevcut yetkilerini getir</summary>
    [HttpGet("personnel/{personnelId}/permissions")]
    public async Task<IActionResult> GetPersonnelPermissions(int customerId, int personnelId)
    {
        var personnel = await _db.CustomerPersonnel
            .Include(p => p.Permissions)
            .FirstOrDefaultAsync(p => p.Id == personnelId && p.CustomerId == customerId);

        if (personnel == null) return NotFound("Personel bulunamadı.");

        var result = personnel.Permissions.Select(p =>
        {
            var permType = CustomerPermissionTypes.GetById(p.PermissionTypeId);
            var scope = PermissionScopes.GetById(p.ScopeId);
            var moduleId = CustomerPermissionTypes.GetModuleId(p.PermissionTypeId);
            var module = PortalModules.GetById(moduleId);

            return new PersonnelPermissionDto
            {
                Id = p.Id,
                PermissionTypeId = p.PermissionTypeId,
                PermissionName = permType?.SystemName ?? "Unknown",
                PermissionDescription = permType?.Description,
                PermissionIcon = permType?.Icon,
                ScopeId = p.ScopeId,
                ScopeName = scope?.SystemName,
                IsActive = p.IsActive,
                ValidFrom = p.ValidFrom,
                ValidUntil = p.ValidUntil,
                Description = p.Description,
                ModuleId = moduleId,
                ModuleName = module?.SystemName
            };
        }).ToList();

        return Ok(result);
    }

    /// <summary>Personele toplu yetki ata</summary>
    [HttpPost("personnel/{personnelId}/permissions")]
    public async Task<IActionResult> AssignPermissions(int customerId, int personnelId, [FromBody] AssignPermissionsRequest request)
    {
        // Personel bu müşteriye ait mi?
        var personnel = await _db.CustomerPersonnel
            .Include(p => p.Permissions)
            .FirstOrDefaultAsync(p => p.Id == personnelId && p.CustomerId == customerId);

        if (personnel == null) return NotFound("Personel bulunamadı.");

        // Müşterinin açık modüllerini kontrol et
        var activeModuleIds = await _db.CustomerPortalModules
            .Where(m => m.CustomerId == customerId && m.IsActive)
            .Select(m => m.ModuleId)
            .ToListAsync();

        var currentUserId = GetUserId();
        var addedCount = 0;

        foreach (var permTypeId in request.PermissionTypeIds)
        {
            // Yetki tipi geçerli mi?
            if (CustomerPermissionTypes.GetById(permTypeId) == null) continue;

            // Bu yetki tipi, müşterinin açık modüllerinden birinde mi?
            var moduleId = CustomerPermissionTypes.GetModuleId(permTypeId);
            if (!activeModuleIds.Contains(moduleId)) continue;

            // Zaten atanmış mı?
            var existing = personnel.Permissions.FirstOrDefault(p => p.PermissionTypeId == permTypeId);
            if (existing != null)
            {
                existing.IsActive = true;
                existing.ScopeId = request.ScopeId;
            }
            else
            {
                personnel.Permissions.Add(new CustomerPersonnelPermission
                {
                    PersonnelId = personnelId,
                    PermissionTypeId = permTypeId,
                    ScopeId = request.ScopeId,
                    IsActive = true,
                    CreatedByUserId = currentUserId
                });
                addedCount++;
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { added = addedCount, total = personnel.Permissions.Count });
    }

    /// <summary>Tek bir yetkiyi güncelle</summary>
    [HttpPut("personnel/{personnelId}/permissions/{id}")]
    public async Task<IActionResult> UpdatePermission(int customerId, int personnelId, int id, [FromBody] UpdatePermissionRequest request)
    {
        var permission = await _db.CustomerPersonnelPermissions
            .Include(p => p.Personnel)
            .FirstOrDefaultAsync(p => p.Id == id && p.PersonnelId == personnelId && p.Personnel.CustomerId == customerId);

        if (permission == null) return NotFound("Yetki bulunamadı.");

        if (request.ScopeId.HasValue)
        {
            if (PermissionScopes.GetById(request.ScopeId.Value) == null)
                return BadRequest("Geçersiz kapsam.");
            permission.ScopeId = request.ScopeId.Value;
        }

        if (request.IsActive.HasValue)
            permission.IsActive = request.IsActive.Value;

        if (request.ValidFrom.HasValue)
            permission.ValidFrom = request.ValidFrom.Value;

        if (request.ValidUntil.HasValue)
            permission.ValidUntil = request.ValidUntil.Value;

        if (request.Description != null)
            permission.Description = request.Description;

        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>Yetkiyi kaldır</summary>
    [HttpDelete("personnel/{personnelId}/permissions/{id}")]
    public async Task<IActionResult> RemovePermission(int customerId, int personnelId, int id)
    {
        var permission = await _db.CustomerPersonnelPermissions
            .Include(p => p.Personnel)
            .FirstOrDefaultAsync(p => p.Id == id && p.PersonnelId == personnelId && p.Personnel.CustomerId == customerId);

        if (permission == null) return NotFound("Yetki bulunamadı.");

        _db.CustomerPersonnelPermissions.Remove(permission);
        await _db.SaveChangesAsync();

        return Ok();
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
