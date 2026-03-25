using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnBranchEntityService : ISlnBranchEntityService
{
    private readonly AppDbContext _db;

    public SlnBranchEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnBranch> GetAllQueryable()
        => _db.SlnBranches.AsQueryable();

    public Task<SlnBranch?> GetByIdAsync(int id)
        => _db.SlnBranches.FindAsync(id).AsTask();

    public void Add(SlnBranch entity) => _db.SlnBranches.Add(entity);
    public void Update(SlnBranch entity) => _db.SlnBranches.Update(entity);
    public void Remove(SlnBranch entity) => _db.SlnBranches.Remove(entity);
}
