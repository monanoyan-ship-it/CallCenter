using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class CustomerPersonnelEntityService : ICustomerPersonnelEntityService
{
    private readonly AppDbContext _db;

    public CustomerPersonnelEntityService(AppDbContext db) => _db = db;

    public Task<CustomerPersonnel?> GetByUserIdAsync(int userId)
        => _db.CustomerPersonnel.FirstOrDefaultAsync(p => p.UserId == userId);

    public Task<CustomerPersonnel?> GetByIdAndCustomerAsync(int personnelId, int customerId)
        => _db.CustomerPersonnel
            .FirstOrDefaultAsync(p => p.Id == personnelId && p.CustomerId == customerId);

    public Task<CustomerPersonnel?> GetByIdWithUserAsync(int personnelId, int customerId)
        => _db.CustomerPersonnel
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == personnelId && p.CustomerId == customerId);

    public Task<CustomerPersonnel?> GetByIdWithPermissionsAsync(int personnelId, int customerId)
        => _db.CustomerPersonnel
            .Include(p => p.Permissions)
            .FirstOrDefaultAsync(p => p.Id == personnelId && p.CustomerId == customerId);

    public Task<CustomerPersonnel?> GetCustomerAdminAsync(int customerId)
        => _db.CustomerPersonnel
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.CustomerId == customerId && p.IsCustomerAdmin);

    public Task<int> GetActiveCountAsync(int customerId, bool excludeAdmin = false)
    {
        var query = _db.CustomerPersonnel
            .Where(p => p.CustomerId == customerId && p.IsActive);
        if (excludeAdmin)
            query = query.Where(p => p.CustomerRoleId != CustomerRoles.Ids.FirmaAdmin
                                  && p.CustomerRoleId != CustomerRoles.Ids.EkipLideri);
        return query.CountAsync();
    }

    public Task<int> GetActiveAdminCountAsync(int customerId)
        => _db.CustomerPersonnel
            .CountAsync(p => p.CustomerId == customerId && p.IsCustomerAdmin && p.IsActive);

    public IQueryable<CustomerPersonnel> GetAllQueryable()
        => _db.CustomerPersonnel.AsQueryable();

    public void Add(CustomerPersonnel entity) => _db.CustomerPersonnel.Add(entity);
    public void Update(CustomerPersonnel entity) => _db.CustomerPersonnel.Update(entity);
    public void Remove(CustomerPersonnel entity) => _db.CustomerPersonnel.Remove(entity);
}
