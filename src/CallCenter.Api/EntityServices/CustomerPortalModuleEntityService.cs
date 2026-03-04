using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class CustomerPortalModuleEntityService : ICustomerPortalModuleEntityService
{
    private readonly AppDbContext _db;

    public CustomerPortalModuleEntityService(AppDbContext db) => _db = db;

    public IQueryable<CustomerPortalModule> GetAllQueryable()
        => _db.CustomerPortalModules.AsQueryable();

    public Task<CustomerPortalModule?> GetByCustomerAndModuleAsync(int customerId, int moduleId)
        => _db.CustomerPortalModules
            .FirstOrDefaultAsync(m => m.CustomerId == customerId && m.ModuleId == moduleId);

    public Task<List<int>> GetActiveModuleIdsAsync(int customerId)
        => _db.CustomerPortalModules
            .Where(m => m.CustomerId == customerId && m.IsActive)
            .Select(m => m.ModuleId)
            .ToListAsync();

    public void Add(CustomerPortalModule entity) => _db.CustomerPortalModules.Add(entity);
    public void Remove(CustomerPortalModule entity) => _db.CustomerPortalModules.Remove(entity);
}
