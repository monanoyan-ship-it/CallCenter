using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class PlatformUserEntityService : IPlatformUserEntityService
{
    private readonly AppDbContext _db;

    public PlatformUserEntityService(AppDbContext db) => _db = db;

    public IQueryable<PlatformUser> GetAllQueryable()
        => _db.PlatformUsers.AsQueryable();

    public Task<PlatformUser?> GetByIdAsync(int id)
        => _db.PlatformUsers.FindAsync(id).AsTask();

    public void Add(PlatformUser entity) => _db.PlatformUsers.Add(entity);
    public void Update(PlatformUser entity) => _db.PlatformUsers.Update(entity);
    public void Remove(PlatformUser entity) => _db.PlatformUsers.Remove(entity);
}
