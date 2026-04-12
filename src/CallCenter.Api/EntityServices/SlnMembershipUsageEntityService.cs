using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnMembershipUsageEntityService : ISlnMembershipUsageEntityService
{
    private readonly AppDbContext _db;

    public SlnMembershipUsageEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnMembershipUsage> GetAllQueryable()
        => _db.SlnMembershipUsages.AsQueryable();

    public Task<SlnMembershipUsage?> GetByIdAsync(int id)
        => _db.SlnMembershipUsages.FindAsync(id).AsTask();

    public void Add(SlnMembershipUsage entity) => _db.SlnMembershipUsages.Add(entity);
    public void Update(SlnMembershipUsage entity) => _db.SlnMembershipUsages.Update(entity);
    public void Remove(SlnMembershipUsage entity) => _db.SlnMembershipUsages.Remove(entity);
}
