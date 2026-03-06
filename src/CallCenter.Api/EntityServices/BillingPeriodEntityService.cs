using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class BillingPeriodEntityService : IBillingPeriodEntityService
{
    private readonly AppDbContext _db;

    public BillingPeriodEntityService(AppDbContext db) => _db = db;

    public Task<CustomerBillingPeriod?> GetByIdAsync(int id)
        => _db.CustomerBillingPeriods.FindAsync(id).AsTask();

    public IQueryable<CustomerBillingPeriod> GetAllQueryable()
        => _db.CustomerBillingPeriods.AsQueryable();

    public void Add(CustomerBillingPeriod entity) => _db.CustomerBillingPeriods.Add(entity);
    public void AddRange(IEnumerable<CustomerBillingPeriod> entities) => _db.CustomerBillingPeriods.AddRange(entities);
    public void Update(CustomerBillingPeriod entity) => _db.CustomerBillingPeriods.Update(entity);
    public void Remove(CustomerBillingPeriod entity) => _db.CustomerBillingPeriods.Remove(entity);
}
