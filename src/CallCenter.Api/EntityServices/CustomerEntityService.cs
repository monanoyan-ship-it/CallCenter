using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class CustomerEntityService : ICustomerEntityService
{
    private readonly AppDbContext _db;

    public CustomerEntityService(AppDbContext db) => _db = db;

    public Task<Customer?> GetByIdAsync(int id)
        => _db.Customers.FindAsync(id).AsTask();

    public Task<Customer?> GetByIdWithPersonnelAsync(int id)
        => _db.Customers
            .Include(c => c.Personnel)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<Customer?> GetByIdWithPortalModulesAsync(int id)
        => _db.Customers
            .Include(c => c.PortalModules)
            .FirstOrDefaultAsync(c => c.Id == id);

    public IQueryable<Customer> GetAllQueryable()
        => _db.Customers.AsQueryable();

    public void Add(Customer entity) => _db.Customers.Add(entity);
    public void Update(Customer entity) => _db.Customers.Update(entity);
    public void Remove(Customer entity) => _db.Customers.Remove(entity);
}
