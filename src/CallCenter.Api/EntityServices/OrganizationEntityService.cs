using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class OrganizationEntityService : IOrganizationEntityService
{
    private readonly AppDbContext _db;

    public OrganizationEntityService(AppDbContext db) => _db = db;

    public IQueryable<CustomerOrganizationUnit> GetAllQueryable()
        => _db.CustomerOrganizationUnits.AsQueryable();

    public Task<CustomerOrganizationUnit?> GetByIdAsync(int customerId, int id)
        => _db.CustomerOrganizationUnits
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId);

    public Task<CustomerOrganizationUnit?> GetByIdWithChildrenAsync(int customerId, int id)
        => _db.CustomerOrganizationUnits
            .Include(o => o.Children)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId);

    public Task<List<CustomerOrganizationUnit>> GetTreeDataAsync(int customerId)
        => _db.CustomerOrganizationUnits
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.Personnel)
            .Include(o => o.PersonnelAssignments).ThenInclude(pa => pa.Personnel)
            .Include(o => o.Queues)
            .Include(o => o.SipAccounts)
            .OrderBy(o => o.DisplayOrder).ThenBy(o => o.Name)
            .ToListAsync();

    public Task<bool> ExistsByNameAsync(int customerId, string name, int? parentId, int? excludeId = null)
    {
        var query = _db.CustomerOrganizationUnits
            .Where(o => o.CustomerId == customerId && o.Name == name && o.ParentId == parentId);
        if (excludeId.HasValue)
            query = query.Where(o => o.Id != excludeId.Value);
        return query.AnyAsync();
    }

    public void Add(CustomerOrganizationUnit entity) => _db.CustomerOrganizationUnits.Add(entity);
    public void Update(CustomerOrganizationUnit entity) => _db.CustomerOrganizationUnits.Update(entity);
    public void Remove(CustomerOrganizationUnit entity) => _db.CustomerOrganizationUnits.Remove(entity);

    // ─── PERSONEL ATAMA (JUNCTION) ───

    public async Task<bool> IsPersonnelAssignedAsync(int personnelId, int orgId)
    {
        // Birincil (OrganizationUnitId FK) veya junction uzerinden atanmis mi
        var primaryAssigned = await _db.CustomerPersonnel
            .AnyAsync(p => p.Id == personnelId && p.OrganizationUnitId == orgId);
        if (primaryAssigned) return true;

        return await _db.CustomerPersonnelOrganizationUnits
            .AnyAsync(x => x.PersonnelId == personnelId && x.OrganizationUnitId == orgId);
    }

    public Task<CustomerPersonnelOrganizationUnit?> GetPersonnelAssignmentAsync(int personnelId, int orgId)
        => _db.CustomerPersonnelOrganizationUnits
            .FirstOrDefaultAsync(x => x.PersonnelId == personnelId && x.OrganizationUnitId == orgId);

    public void AddPersonnelAssignment(CustomerPersonnelOrganizationUnit assignment)
        => _db.CustomerPersonnelOrganizationUnits.Add(assignment);

    public void RemovePersonnelAssignment(CustomerPersonnelOrganizationUnit assignment)
        => _db.CustomerPersonnelOrganizationUnits.Remove(assignment);

    public async Task<List<OrgUnitPersonnelDto>> GetAvailablePersonnelAsync(int customerId, int orgId)
    {
        // Zaten atanmis personel ID'leri (birincil + junction)
        var primaryIds = await _db.CustomerPersonnel
            .Where(p => p.CustomerId == customerId && p.OrganizationUnitId == orgId && p.IsActive)
            .Select(p => p.Id)
            .ToListAsync();

        var junctionIds = await _db.CustomerPersonnelOrganizationUnits
            .Where(x => x.OrganizationUnitId == orgId)
            .Select(x => x.PersonnelId)
            .ToListAsync();

        var assignedIds = primaryIds.Union(junctionIds).ToHashSet();

        // Musterinin aktif ve henuz bu orga atanmamis personelleri
        return await _db.CustomerPersonnel
            .Where(p => p.CustomerId == customerId && p.IsActive && !assignedIds.Contains(p.Id))
            .Include(p => p.User)
            .Select(p => new OrgUnitPersonnelDto
            {
                Id = p.Id,
                FullName = p.User.FullName,
                Title = p.Title,
                CustomerRoleName = CustomerRoles.GetById(p.CustomerRoleId) != null
                    ? CustomerRoles.GetById(p.CustomerRoleId)!.Description : null
            })
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }
}
