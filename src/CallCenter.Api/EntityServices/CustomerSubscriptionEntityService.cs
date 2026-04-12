using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CustomerSubscriptionEntityService : ICustomerSubscriptionEntityService
{
    private readonly AppDbContext _db;

    public CustomerSubscriptionEntityService(AppDbContext db) => _db = db;

    public IQueryable<CustomerSubscription> GetAllQueryable()
        => _db.CustomerSubscriptions.AsQueryable();

    public Task<CustomerSubscription?> GetByIdAsync(int id)
        => _db.CustomerSubscriptions.FindAsync(id).AsTask();

    public void Add(CustomerSubscription entity) => _db.CustomerSubscriptions.Add(entity);
    public void Update(CustomerSubscription entity) => _db.CustomerSubscriptions.Update(entity);
    public void Remove(CustomerSubscription entity) => _db.CustomerSubscriptions.Remove(entity);
}
