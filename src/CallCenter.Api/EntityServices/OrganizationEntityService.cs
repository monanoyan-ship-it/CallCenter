using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
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
}
