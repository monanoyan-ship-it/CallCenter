using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class RetentionPolicyEntityService : IRetentionPolicyEntityService
{
    private readonly AppDbContext _db;

    public RetentionPolicyEntityService(AppDbContext db) => _db = db;

    public IQueryable<RetentionPolicy> GetAllQueryable() => _db.RetentionPolicies.AsQueryable();
    public Task<RetentionPolicy?> GetByIdAsync(int id) => _db.RetentionPolicies.FindAsync(id).AsTask();
    public void Add(RetentionPolicy entity) => _db.RetentionPolicies.Add(entity);
    public void Update(RetentionPolicy entity) => _db.RetentionPolicies.Update(entity);
    public void Remove(RetentionPolicy entity) => _db.RetentionPolicies.Remove(entity);
}
