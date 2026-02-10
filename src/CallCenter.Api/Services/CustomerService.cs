using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db)
    {
        _db = db;
    }

    // ═══════════════════════════════════════════════════════════
    // CUSTOMERS
    // ═══════════════════════════════════════════════════════════

    public async Task<PagedResult<CustomerListDto>> GetAllAsync(int page, int pageSize, string? search)
    {
        var query = _db.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(s)
                                  || (c.Email != null && c.Email.ToLower().Contains(s))
                                  || (c.Phone != null && c.Phone.Contains(s)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerListDto
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                Email = c.Email,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                PersonnelCount = c.Personnel.Count,
                QueueCount = c.Queues.Count,
                SipAccountCount = c.SipAccounts.Count
            })
            .ToListAsync();

        return new PagedResult<CustomerListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CustomerDetailDto?> GetByIdAsync(int id)
    {
        var c = await _db.Customers
            .Include(x => x.Personnel)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (c == null) return null;

        return new CustomerDetailDto
        {
            Id = c.Id,
            Name = c.Name,
            TaxNumber = c.TaxNumber,
            Address = c.Address,
            Phone = c.Phone,
            Email = c.Email,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            Personnel = c.Personnel.Select(p => new PersonnelSimpleDto
            {
                Id = p.Id,
                FullName = p.User.FullName,
                Title = p.Title
            }).ToList()
        };
    }

    public async Task<int> CreateAsync(CustomerCreateDto dto)
    {
        var customer = new Customer
        {
            Name = dto.Name,
            TaxNumber = dto.TaxNumber,
            Address = dto.Address,
            Phone = dto.Phone,
            Email = dto.Email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        // Varsayilan portal modullerini otomatik ata
        foreach (var module in PortalModules.Defaults)
        {
            _db.CustomerPortalModules.Add(new CustomerPortalModule
            {
                CustomerId = customer.Id,
                ModuleId = module.Id,
                IsActive = true,
                ActivatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        return customer.Id;
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, CustomerUpdateDto dto)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return (false, "Musteri bulunamadi.");

        customer.Name = dto.Name;
        customer.TaxNumber = dto.TaxNumber;
        customer.Address = dto.Address;
        customer.Phone = dto.Phone;
        customer.Email = dto.Email;
        customer.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return (false, "Musteri bulunamadi.");

        customer.IsActive = false;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    // ═══════════════════════════════════════════════════════════
    // CUSTOMER PERMISSIONS — MODULES
    // ═══════════════════════════════════════════════════════════

    public async Task<object?> GetCustomerModulesAsync(int customerId)
    {
        var customer = await _db.Customers
            .Include(c => c.PortalModules)
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null) return null;

        return PortalModules.All.Select(m =>
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
    }

    public async Task<(bool Success, string? Error)> AssignModulesAsync(int customerId, AssignModulesRequest request)
    {
        var customer = await _db.Customers
            .Include(c => c.PortalModules)
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null) return (false, "Müşteri bulunamadı.");

        foreach (var moduleId in request.ModuleIds)
        {
            if (PortalModules.GetById(moduleId) == null) continue;

            var existing = customer.PortalModules.FirstOrDefault(m => m.ModuleId == moduleId);
            if (existing != null)
            {
                existing.IsActive = true;
                existing.DeactivatedAt = null;
            }
            else
            {
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
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeactivateModuleAsync(int customerId, int moduleId)
    {
        var module = await _db.CustomerPortalModules
            .FirstOrDefaultAsync(m => m.CustomerId == customerId && m.ModuleId == moduleId);

        if (module == null) return (false, "Modül ataması bulunamadı.");

        module.IsActive = false;
        module.DeactivatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    // ═══════════════════════════════════════════════════════════
    // CUSTOMER PERMISSIONS — PERMISSION TYPES
    // ═══════════════════════════════════════════════════════════

    public async Task<object> GetAvailablePermissionTypesAsync(int customerId)
    {
        var activeModuleIds = await _db.CustomerPortalModules
            .Where(m => m.CustomerId == customerId && m.IsActive)
            .Select(m => m.ModuleId)
            .ToListAsync();

        return activeModuleIds
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
    }

    // ═══════════════════════════════════════════════════════════
    // CUSTOMER PERMISSIONS — PERSONNEL PERMISSIONS
    // ═══════════════════════════════════════════════════════════

    public async Task<object?> GetPersonnelPermissionsAsync(int customerId, int personnelId)
    {
        var personnel = await _db.CustomerPersonnel
            .Include(p => p.Permissions)
            .FirstOrDefaultAsync(p => p.Id == personnelId && p.CustomerId == customerId);

        if (personnel == null) return null;

        return personnel.Permissions.Select(p =>
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
    }

    public async Task<(bool Success, object? Result, string? Error)> AssignPermissionsAsync(int customerId, int personnelId, AssignPermissionsRequest request, int currentUserId)
    {
        var personnel = await _db.CustomerPersonnel
            .Include(p => p.Permissions)
            .FirstOrDefaultAsync(p => p.Id == personnelId && p.CustomerId == customerId);

        if (personnel == null) return (false, null, "Personel bulunamadı.");

        var activeModuleIds = await _db.CustomerPortalModules
            .Where(m => m.CustomerId == customerId && m.IsActive)
            .Select(m => m.ModuleId)
            .ToListAsync();

        var addedCount = 0;

        foreach (var permTypeId in request.PermissionTypeIds)
        {
            if (CustomerPermissionTypes.GetById(permTypeId) == null) continue;

            var moduleId = CustomerPermissionTypes.GetModuleId(permTypeId);
            if (!activeModuleIds.Contains(moduleId)) continue;

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
        return (true, new { added = addedCount, total = personnel.Permissions.Count }, null);
    }

    public async Task<(bool Success, string? Error)> UpdatePermissionAsync(int customerId, int personnelId, int id, UpdatePermissionRequest request)
    {
        var permission = await _db.CustomerPersonnelPermissions
            .Include(p => p.Personnel)
            .FirstOrDefaultAsync(p => p.Id == id && p.PersonnelId == personnelId && p.Personnel.CustomerId == customerId);

        if (permission == null) return (false, "Yetki bulunamadı.");

        if (request.ScopeId.HasValue)
        {
            if (PermissionScopes.GetById(request.ScopeId.Value) == null)
                return (false, "Geçersiz kapsam.");
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
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemovePermissionAsync(int customerId, int personnelId, int id)
    {
        var permission = await _db.CustomerPersonnelPermissions
            .Include(p => p.Personnel)
            .FirstOrDefaultAsync(p => p.Id == id && p.PersonnelId == personnelId && p.Personnel.CustomerId == customerId);

        if (permission == null) return (false, "Yetki bulunamadı.");

        _db.CustomerPersonnelPermissions.Remove(permission);
        await _db.SaveChangesAsync();

        return (true, null);
    }
}
