using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class CustomerProductEntityService : ICustomerProductEntityService
{
    private readonly AppDbContext _db;

    public CustomerProductEntityService(AppDbContext db) => _db = db;

    public Task<CustomerProduct?> GetByIdAsync(int id)
        => _db.CustomerProducts.FindAsync(id).AsTask();

    public Task<List<CustomerProduct>> GetByCustomerIdAsync(int customerId)
        => _db.CustomerProducts.Where(cp => cp.CustomerId == customerId).ToListAsync();

    public IQueryable<CustomerProduct> GetAllQueryable()
        => _db.CustomerProducts.AsQueryable();

    public void Add(CustomerProduct entity) => _db.CustomerProducts.Add(entity);
    public void Update(CustomerProduct entity) => _db.CustomerProducts.Update(entity);
    public void Remove(CustomerProduct entity) => _db.CustomerProducts.Remove(entity);
}
