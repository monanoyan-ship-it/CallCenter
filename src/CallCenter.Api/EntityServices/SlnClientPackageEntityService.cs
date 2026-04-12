using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnClientPackageEntityService : ISlnClientPackageEntityService
{
    private readonly AppDbContext _db;

    public SlnClientPackageEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnClientPackage> GetAllQueryable()
        => _db.SlnClientPackages.AsQueryable();

    public Task<SlnClientPackage?> GetByIdAsync(int id)
        => _db.SlnClientPackages.FindAsync(id).AsTask();

    public void Add(SlnClientPackage entity) => _db.SlnClientPackages.Add(entity);
    public void Update(SlnClientPackage entity) => _db.SlnClientPackages.Update(entity);
    public void Remove(SlnClientPackage entity) => _db.SlnClientPackages.Remove(entity);
}
