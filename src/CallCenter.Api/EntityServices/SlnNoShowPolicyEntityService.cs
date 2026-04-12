using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnNoShowPolicyEntityService : ISlnNoShowPolicyEntityService
{
    private readonly AppDbContext _db;

    public SlnNoShowPolicyEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnNoShowPolicy> GetAllQueryable()
        => _db.SlnNoShowPolicies.AsQueryable();

    public Task<SlnNoShowPolicy?> GetByIdAsync(int id)
        => _db.SlnNoShowPolicies.FindAsync(id).AsTask();

    public void Add(SlnNoShowPolicy entity) => _db.SlnNoShowPolicies.Add(entity);
    public void Update(SlnNoShowPolicy entity) => _db.SlnNoShowPolicies.Update(entity);
    public void Remove(SlnNoShowPolicy entity) => _db.SlnNoShowPolicies.Remove(entity);
}
