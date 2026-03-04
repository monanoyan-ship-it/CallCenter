using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class CustomerPersonnelPermissionEntityService : ICustomerPersonnelPermissionEntityService
{
    private readonly AppDbContext _db;

    public CustomerPersonnelPermissionEntityService(AppDbContext db) => _db = db;

    public IQueryable<CustomerPersonnelPermission> GetAllQueryable()
        => _db.CustomerPersonnelPermissions.AsQueryable();

    public Task<CustomerPersonnelPermission?> GetByIdAsync(int id, int personnelId, int customerId)
        => _db.CustomerPersonnelPermissions
            .Include(p => p.Personnel)
            .FirstOrDefaultAsync(p => p.Id == id && p.PersonnelId == personnelId && p.Personnel.CustomerId == customerId);

    public Task<List<CustomerPersonnelPermission>> GetByPersonnelIdAsync(int personnelId)
        => _db.CustomerPersonnelPermissions
            .Where(p => p.PersonnelId == personnelId)
            .ToListAsync();

    public void Add(CustomerPersonnelPermission entity) => _db.CustomerPersonnelPermissions.Add(entity);
    public void Remove(CustomerPersonnelPermission entity) => _db.CustomerPersonnelPermissions.Remove(entity);
    public void RemoveRange(IEnumerable<CustomerPersonnelPermission> entities) => _db.CustomerPersonnelPermissions.RemoveRange(entities);
}
