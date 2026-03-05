using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class OrganizationFactory : IOrganizationFactory
{
    private readonly IOrganizationEntityService _orgEs;
    private readonly IUnitOfWork _uow;

    public OrganizationFactory(IOrganizationEntityService orgEs, IUnitOfWork uow)
    {
        _orgEs = orgEs;
        _uow = uow;
    }

    public async Task<List<OrgUnitTreeDto>> GetTreeAsync(int customerId)
    {
        var units = await _orgEs.GetTreeDataAsync(customerId);
        var rootUnits = units.Where(o => o.ParentId == null).ToList();
        return rootUnits.Select(r => BuildTree(r, units)).ToList();
    }

    private static OrgUnitTreeDto BuildTree(CustomerOrganizationUnit node, List<CustomerOrganizationUnit> allUnits)
    {
        var type = OrganizationUnitTypes.GetById(node.TypeId);
        var children = allUnits.Where(o => o.ParentId == node.Id).ToList();

        // Personel sayisi: birincil (OrganizationUnitId FK) + junction atamalari
        var primaryCount = node.Personnel.Count(p => p.IsActive);
        var junctionCount = node.PersonnelAssignments.Count(pa => pa.Personnel.IsActive);
        // Birincil ve junction'da ayni personel olabilir, distinct yapiyoruz
        var allPersonnelIds = node.Personnel.Where(p => p.IsActive).Select(p => p.Id)
            .Union(node.PersonnelAssignments.Where(pa => pa.Personnel.IsActive).Select(pa => pa.PersonnelId))
            .Distinct().Count();

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
            PersonnelCount = allPersonnelIds,
            QueueCount = node.Queues.Count(q => q.IsActive),
            SipAccountCount = node.SipAccounts.Count(s => s.IsActive),
            IsActive = node.IsActive,
            DisplayOrder = node.DisplayOrder,
            Children = children.Select(c => BuildTree(c, allUnits)).ToList()
        };
    }

    public async Task<List<OrgUnitListDto>> GetListAsync(int customerId)
    {
        return await _orgEs.GetAllQueryable()
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
                ChildCount = o.Children.Count,
                PersonnelCount = o.Personnel.Count(p => p.IsActive) +
                    o.PersonnelAssignments.Count(pa => pa.Personnel.IsActive),
                IsActive = o.IsActive
            })
            .ToListAsync();
    }

    public async Task<OrgUnitDetailDto?> GetByIdAsync(int customerId, int id)
    {
        var unit = await _orgEs.GetAllQueryable()
            .Where(o => o.CustomerId == customerId && o.Id == id)
            .Include(o => o.Parent)
            .Include(o => o.Personnel.Where(p => p.IsActive))
                .ThenInclude(p => p.User)
            .Include(o => o.Personnel.Where(p => p.IsActive))
                .ThenInclude(p => p.ReportsToPersonnel)
                    .ThenInclude(r => r!.User)
            .Include(o => o.PersonnelAssignments)
                .ThenInclude(pa => pa.Personnel)
                    .ThenInclude(p => p.User)
            .Include(o => o.PersonnelAssignments)
                .ThenInclude(pa => pa.Personnel)
                    .ThenInclude(p => p.ReportsToPersonnel)
                        .ThenInclude(r => r!.User)
            .Include(o => o.Children)
            .FirstOrDefaultAsync();

        if (unit == null) return null;

        var type = OrganizationUnitTypes.GetById(unit.TypeId);

        // Birincil + junction personellerini birlestir (distinct)
        var primaryPersonnel = unit.Personnel.Where(p => p.IsActive).Select(p => new OrgUnitPersonnelDto
        {
            Id = p.Id,
            FullName = p.User.FullName,
            Title = p.Title,
            CustomerRoleName = CustomerRoles.GetById(p.CustomerRoleId)?.Description,
            ReportsToPersonnelId = p.ReportsToPersonnelId,
            ReportsToName = p.ReportsToPersonnel?.User.FullName,
            IsPrimaryAssignment = true
        });

        var junctionPersonnel = unit.PersonnelAssignments
            .Where(pa => pa.Personnel.IsActive)
            .Select(pa => new OrgUnitPersonnelDto
            {
                Id = pa.Personnel.Id,
                FullName = pa.Personnel.User.FullName,
                Title = pa.Personnel.Title,
                CustomerRoleName = CustomerRoles.GetById(pa.Personnel.CustomerRoleId)?.Description,
                ReportsToPersonnelId = pa.Personnel.ReportsToPersonnelId,
                ReportsToName = pa.Personnel.ReportsToPersonnel?.User.FullName,
                IsPrimaryAssignment = false
            });

        // Distinct by Id: birincil oncelikli
        var allPersonnel = primaryPersonnel
            .Concat(junctionPersonnel)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();

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
            Address = unit.Address,
            Phone = unit.Phone,
            Email = unit.Email,
            DisplayOrder = unit.DisplayOrder,
            IsActive = unit.IsActive,
            CreatedAt = unit.CreatedAt,
            Personnel = allPersonnel,
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
        if (dto.ParentId.HasValue)
        {
            var parent = await _orgEs.GetByIdAsync(customerId, dto.ParentId.Value);
            if (parent == null)
                return (false, "Ust birim bulunamadi.");
        }

        if (await _orgEs.ExistsByNameAsync(customerId, dto.Name, dto.ParentId))
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
            DisplayOrder = dto.DisplayOrder
        };

        _orgEs.Add(unit);
        await _uow.SaveChangesAsync();

        return (true, new { unit.Id });
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int customerId, int id, OrgUnitUpdateDto dto)
    {
        var unit = await _orgEs.GetByIdAsync(customerId, id);
        if (unit == null)
            return (false, "Birim bulunamadi.");

        if (dto.ParentId.HasValue)
        {
            if (dto.ParentId.Value == id)
                return (false, "Birim kendi ustune atanamaz.");

            if (await IsDescendantAsync(id, dto.ParentId.Value))
                return (false, "Dongusel iliski tespit edildi. Bir alt birim ust birim olarak atanamaz.");

            var parent = await _orgEs.GetByIdAsync(customerId, dto.ParentId.Value);
            if (parent == null)
                return (false, "Ust birim bulunamadi.");
        }

        if (await _orgEs.ExistsByNameAsync(customerId, dto.Name, dto.ParentId, excludeId: id))
            return (false, "Bu isimde bir birim zaten mevcut.");

        unit.Name = dto.Name;
        unit.Code = dto.Code;
        unit.TypeId = dto.TypeId;
        unit.ParentId = dto.ParentId;
        unit.Address = dto.Address;
        unit.Phone = dto.Phone;
        unit.Email = dto.Email;
        unit.DisplayOrder = dto.DisplayOrder;
        unit.IsActive = dto.IsActive;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int customerId, int id)
    {
        var unit = await _orgEs.GetByIdWithChildrenAsync(customerId, id);
        if (unit == null)
            return (false, "Birim bulunamadi.");

        if (unit.Children.Any())
            return (false, "Alt birimleri olan bir birim silinemez. Once alt birimleri kaldirin.");

        _orgEs.Remove(unit);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<OrgUnitListDto>> GetPotentialParentsAsync(int customerId, int? excludeId)
    {
        var query = _orgEs.GetAllQueryable()
            .Where(o => o.CustomerId == customerId && o.IsActive);

        if (excludeId.HasValue)
        {
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

    // ─── PERSONEL ATAMA ───

    public async Task<(bool Success, string? Error)> AssignPersonnelAsync(int customerId, int orgId, int personnelId)
    {
        var unit = await _orgEs.GetByIdAsync(customerId, orgId);
        if (unit == null)
            return (false, "Organizasyon birimi bulunamadi.");

        // Zaten atanmis mi kontrol et (hem birincil hem junction)
        var alreadyAssigned = await _orgEs.IsPersonnelAssignedAsync(personnelId, orgId);
        if (alreadyAssigned)
            return (false, "Bu personel zaten bu birime atanmis.");

        _orgEs.AddPersonnelAssignment(new CustomerPersonnelOrganizationUnit
        {
            PersonnelId = personnelId,
            OrganizationUnitId = orgId
        });
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemovePersonnelAsync(int customerId, int orgId, int personnelId)
    {
        var assignment = await _orgEs.GetPersonnelAssignmentAsync(personnelId, orgId);
        if (assignment == null)
            return (false, "Atama bulunamadi.");

        _orgEs.RemovePersonnelAssignment(assignment);
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<List<OrgUnitPersonnelDto>> GetAvailablePersonnelAsync(int customerId, int orgId)
    {
        return await _orgEs.GetAvailablePersonnelAsync(customerId, orgId);
    }

    // ─── PRIVATE ───

    private async Task<bool> IsDescendantAsync(int nodeId, int targetId)
    {
        var descendantIds = await GetDescendantIdsAsync(nodeId);
        return descendantIds.Contains(targetId);
    }

    private async Task<HashSet<int>> GetDescendantIdsAsync(int nodeId)
    {
        var allUnits = await _orgEs.GetAllQueryable()
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
