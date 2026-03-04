using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class OrganizationService : IOrganizationService
{
    private readonly AppDbContext _db;

    public OrganizationService(AppDbContext db) => _db = db;

    public async Task<List<OrgUnitTreeDto>> GetTreeAsync(int customerId)
    {
        var units = await _db.CustomerOrganizationUnits
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.Personnel)
            .Include(o => o.Queues)
            .Include(o => o.SipAccounts)
            .OrderBy(o => o.DisplayOrder).ThenBy(o => o.Name)
            .ToListAsync();

        var rootUnits = units.Where(o => o.ParentId == null).ToList();
        return rootUnits.Select(r => BuildTree(r, units)).ToList();
    }

    private static OrgUnitTreeDto BuildTree(CustomerOrganizationUnit node, List<CustomerOrganizationUnit> allUnits)
    {
        var type = OrganizationUnitTypes.GetById(node.TypeId);
        var children = allUnits.Where(o => o.ParentId == node.Id).ToList();

        return new OrgUnitTreeDto
        {
            Id = node.Id,
            Name = node.Name,
            Code = node.Code,
            TypeId = node.TypeId,
            TypeName = type?.Description ?? "Bilinmiyor",
            TypeIcon = type?.Icon,
            TypeCssClass = type?.CssClass,
            ParentId = node.ParentId,
            PersonnelCount = node.Personnel.Count(p => p.IsActive),
            QueueCount = node.Queues.Count(q => q.IsActive),
            SipAccountCount = node.SipAccounts.Count(s => s.IsActive),
            IsActive = node.IsActive,
            DisplayOrder = node.DisplayOrder,
            Children = children.Select(c => BuildTree(c, allUnits)).ToList()
        };
    }

    public async Task<List<OrgUnitListDto>> GetListAsync(int customerId)
    {
        return await _db.CustomerOrganizationUnits
            .Where(o => o.CustomerId == customerId)
            .OrderBy(o => o.DisplayOrder).ThenBy(o => o.Name)
            .Select(o => new OrgUnitListDto
            {
                Id = o.Id,
                Name = o.Name,
                Code = o.Code,
                TypeId = o.TypeId,
                ParentId = o.ParentId,
                ParentName = o.Parent != null ? o.Parent.Name : null,
                ManagerName = o.ManagerPersonnel != null ? o.ManagerPersonnel.User.FullName : null,
                ChildCount = o.Children.Count,
                PersonnelCount = o.Personnel.Count(p => p.IsActive),
                IsActive = o.IsActive
            })
            .ToListAsync();
    }

    public async Task<OrgUnitDetailDto?> GetByIdAsync(int customerId, int id)
    {
        var unit = await _db.CustomerOrganizationUnits
            .Where(o => o.CustomerId == customerId && o.Id == id)
            .Include(o => o.Parent)
            .Include(o => o.ManagerPersonnel).ThenInclude(m => m!.User)
            .Include(o => o.Personnel.Where(p => p.IsActive)).ThenInclude(p => p.User)
            .Include(o => o.Children)
            .FirstOrDefaultAsync();

        if (unit == null) return null;

        var type = OrganizationUnitTypes.GetById(unit.TypeId);

        return new OrgUnitDetailDto
        {
            Id = unit.Id,
            Uid = unit.Uid,
            Name = unit.Name,
            Code = unit.Code,
            TypeId = unit.TypeId,
            TypeName = type?.Description ?? "Bilinmiyor",
            TypeIcon = type?.Icon,
            ParentId = unit.ParentId,
            ParentName = unit.Parent?.Name,
            ManagerPersonnelId = unit.ManagerPersonnelId,
            ManagerName = unit.ManagerPersonnel?.User.FullName,
            Address = unit.Address,
            Phone = unit.Phone,
            Email = unit.Email,
            DisplayOrder = unit.DisplayOrder,
            IsActive = unit.IsActive,
            CreatedAt = unit.CreatedAt,
            Personnel = unit.Personnel.Where(p => p.IsActive).Select(p => new OrgUnitPersonnelDto
            {
                Id = p.Id,
                FullName = p.User.FullName,
                Title = p.Title,
                CustomerRoleName = CustomerRoles.GetById(p.CustomerRoleId)?.Description
            }).ToList(),
            Children = unit.Children.Select(c => new OrgUnitChildDto
            {
                Id = c.Id,
                Name = c.Name,
                TypeName = OrganizationUnitTypes.GetById(c.TypeId)?.Description ?? "Bilinmiyor",
                PersonnelCount = c.Personnel.Count(p => p.IsActive)
            }).ToList()
        };
    }

    public async Task<(bool Success, object Result)> CreateAsync(int customerId, OrgUnitCreateDto dto)
    {
        // ParentId dogrulamasi
        if (dto.ParentId.HasValue)
        {
            var parent = await _db.CustomerOrganizationUnits
                .FirstOrDefaultAsync(o => o.Id == dto.ParentId.Value && o.CustomerId == customerId);
            if (parent == null)
                return (false, "Ust birim bulunamadi.");
        }

        // Unique kontrol
        var exists = await _db.CustomerOrganizationUnits
            .AnyAsync(o => o.CustomerId == customerId && o.Name == dto.Name && o.ParentId == dto.ParentId);
        if (exists)
            return (false, "Bu isimde bir birim zaten mevcut.");

        var unit = new CustomerOrganizationUnit
        {
            CustomerId = customerId,
            Name = dto.Name,
            Code = dto.Code,
            TypeId = dto.TypeId,
            ParentId = dto.ParentId,
            Address = dto.Address,
            Phone = dto.Phone,
            Email = dto.Email,
            ManagerPersonnelId = dto.ManagerPersonnelId,
            DisplayOrder = dto.DisplayOrder
        };

        _db.CustomerOrganizationUnits.Add(unit);
        await _db.SaveChangesAsync();

        return (true, new { unit.Id });
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int customerId, int id, OrgUnitUpdateDto dto)
    {
        var unit = await _db.CustomerOrganizationUnits
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId);
        if (unit == null)
            return (false, "Birim bulunamadi.");

        // ParentId dongusel iliskiyi kontrol et
        if (dto.ParentId.HasValue)
        {
            if (dto.ParentId.Value == id)
                return (false, "Birim kendi ustune atanamaz.");

            if (await IsDescendantAsync(id, dto.ParentId.Value))
                return (false, "Dongusel iliski tespit edildi. Bir alt birim ust birim olarak atanamaz.");

            var parent = await _db.CustomerOrganizationUnits
                .FirstOrDefaultAsync(o => o.Id == dto.ParentId.Value && o.CustomerId == customerId);
            if (parent == null)
                return (false, "Ust birim bulunamadi.");
        }

        // Unique kontrol (kendisi haric)
        var exists = await _db.CustomerOrganizationUnits
            .AnyAsync(o => o.CustomerId == customerId && o.Name == dto.Name && o.ParentId == dto.ParentId && o.Id != id);
        if (exists)
            return (false, "Bu isimde bir birim zaten mevcut.");

        unit.Name = dto.Name;
        unit.Code = dto.Code;
        unit.TypeId = dto.TypeId;
        unit.ParentId = dto.ParentId;
        unit.Address = dto.Address;
        unit.Phone = dto.Phone;
        unit.Email = dto.Email;
        unit.ManagerPersonnelId = dto.ManagerPersonnelId;
        unit.DisplayOrder = dto.DisplayOrder;
        unit.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int customerId, int id)
    {
        var unit = await _db.CustomerOrganizationUnits
            .Include(o => o.Children)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId);
        if (unit == null)
            return (false, "Birim bulunamadi.");

        if (unit.Children.Any())
            return (false, "Alt birimleri olan bir birim silinemez. Once alt birimleri kaldirin.");

        _db.CustomerOrganizationUnits.Remove(unit);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<OrgUnitListDto>> GetPotentialParentsAsync(int customerId, int? excludeId)
    {
        var query = _db.CustomerOrganizationUnits
            .Where(o => o.CustomerId == customerId && o.IsActive);

        if (excludeId.HasValue)
        {
            // Kendini ve alt birimlerini haric tut
            var descendantIds = await GetDescendantIdsAsync(excludeId.Value);
            descendantIds.Add(excludeId.Value);
            query = query.Where(o => !descendantIds.Contains(o.Id));
        }

        return await query
            .OrderBy(o => o.DisplayOrder).ThenBy(o => o.Name)
            .Select(o => new OrgUnitListDto
            {
                Id = o.Id,
                Name = o.Name,
                Code = o.Code,
                TypeId = o.TypeId,
                ParentId = o.ParentId,
                ParentName = o.Parent != null ? o.Parent.Name : null,
                IsActive = o.IsActive
            })
            .ToListAsync();
    }

    // Dongusel iliski kontrolu: targetId, nodeId'nin alt birimlerinden biri mi?
    private async Task<bool> IsDescendantAsync(int nodeId, int targetId)
    {
        var descendantIds = await GetDescendantIdsAsync(nodeId);
        return descendantIds.Contains(targetId);
    }

    private async Task<HashSet<int>> GetDescendantIdsAsync(int nodeId)
    {
        var allUnits = await _db.CustomerOrganizationUnits
            .Select(o => new { o.Id, o.ParentId })
            .ToListAsync();

        var descendants = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(nodeId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var children = allUnits.Where(o => o.ParentId == current).Select(o => o.Id);
            foreach (var childId in children)
            {
                if (descendants.Add(childId))
                    queue.Enqueue(childId);
            }
        }

        return descendants;
    }
}
