using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class CustomerServiceSubscriptionEntityService : ICustomerServiceSubscriptionEntityService
{
    private readonly AppDbContext _db;

    public CustomerServiceSubscriptionEntityService(AppDbContext db) => _db = db;

    public Task<CustomerServiceSubscription?> GetByIdAsync(int id)
        => _db.CustomerServiceSubscriptions.FindAsync(id).AsTask();

    public Task<CustomerServiceSubscription?> GetByUidAsync(Guid uid)
        => _db.CustomerServiceSubscriptions.FirstOrDefaultAsync(s => s.Uid == uid);

    public IQueryable<CustomerServiceSubscription> GetAllQueryable()
        => _db.CustomerServiceSubscriptions.AsQueryable();

    public void Add(CustomerServiceSubscription entity) => _db.CustomerServiceSubscriptions.Add(entity);
    public void Update(CustomerServiceSubscription entity) => _db.CustomerServiceSubscriptions.Update(entity);
}
