using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnMembershipPlanEntityService : ISlnMembershipPlanEntityService
{
    private readonly AppDbContext _db;

    public SlnMembershipPlanEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnMembershipPlan> GetAllQueryable()
        => _db.SlnMembershipPlans.AsQueryable();

    public Task<SlnMembershipPlan?> GetByIdAsync(int id)
        => _db.SlnMembershipPlans.FindAsync(id).AsTask();

    public void Add(SlnMembershipPlan entity) => _db.SlnMembershipPlans.Add(entity);
    public void Update(SlnMembershipPlan entity) => _db.SlnMembershipPlans.Update(entity);
    public void Remove(SlnMembershipPlan entity) => _db.SlnMembershipPlans.Remove(entity);
}
