using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnMembershipPlanServiceEntityService : ISlnMembershipPlanServiceEntityService
{
    private readonly AppDbContext _db;

    public SlnMembershipPlanServiceEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnMembershipPlanService> GetAllQueryable()
        => _db.SlnMembershipPlanServices.AsQueryable();

    public Task<SlnMembershipPlanService?> GetByIdAsync(int id)
        => _db.SlnMembershipPlanServices.FindAsync(id).AsTask();

    public void Add(SlnMembershipPlanService entity) => _db.SlnMembershipPlanServices.Add(entity);
    public void Update(SlnMembershipPlanService entity) => _db.SlnMembershipPlanServices.Update(entity);
    public void Remove(SlnMembershipPlanService entity) => _db.SlnMembershipPlanServices.Remove(entity);
    public void RemoveRange(IEnumerable<SlnMembershipPlanService> entities) => _db.SlnMembershipPlanServices.RemoveRange(entities);
}
