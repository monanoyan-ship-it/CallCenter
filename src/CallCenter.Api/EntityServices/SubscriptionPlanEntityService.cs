using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SubscriptionPlanEntityService : ISubscriptionPlanEntityService
{
    private readonly AppDbContext _db;

    public SubscriptionPlanEntityService(AppDbContext db) => _db = db;

    public IQueryable<SubscriptionPlan> GetAllQueryable()
        => _db.SubscriptionPlans.AsQueryable();

    public Task<SubscriptionPlan?> GetByIdAsync(int id)
        => _db.SubscriptionPlans.FindAsync(id).AsTask();

    public void Add(SubscriptionPlan entity) => _db.SubscriptionPlans.Add(entity);
    public void Update(SubscriptionPlan entity) => _db.SubscriptionPlans.Update(entity);
    public void Remove(SubscriptionPlan entity) => _db.SubscriptionPlans.Remove(entity);
}
