using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnServiceSessionPlanEntityService : ISlnServiceSessionPlanEntityService
{
    private readonly AppDbContext _db;

    public SlnServiceSessionPlanEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnServiceSessionPlan> GetAllQueryable()
        => _db.SlnServiceSessionPlans.AsQueryable();

    public Task<SlnServiceSessionPlan?> GetByIdAsync(int id)
        => _db.SlnServiceSessionPlans.FindAsync(id).AsTask();

    public void Add(SlnServiceSessionPlan entity) => _db.SlnServiceSessionPlans.Add(entity);
    public void Update(SlnServiceSessionPlan entity) => _db.SlnServiceSessionPlans.Update(entity);
    public void Remove(SlnServiceSessionPlan entity) => _db.SlnServiceSessionPlans.Remove(entity);
}
