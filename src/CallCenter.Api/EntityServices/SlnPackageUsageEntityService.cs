using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnPackageUsageEntityService : ISlnPackageUsageEntityService
{
    private readonly AppDbContext _db;

    public SlnPackageUsageEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnPackageUsage> GetAllQueryable()
        => _db.SlnPackageUsages.AsQueryable();

    public Task<SlnPackageUsage?> GetByIdAsync(int id)
        => _db.SlnPackageUsages.FindAsync(id).AsTask();

    public void Add(SlnPackageUsage entity) => _db.SlnPackageUsages.Add(entity);
    public void Update(SlnPackageUsage entity) => _db.SlnPackageUsages.Update(entity);
    public void Remove(SlnPackageUsage entity) => _db.SlnPackageUsages.Remove(entity);
}
