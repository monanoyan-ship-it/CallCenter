using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class PlatformUserSalonEntityService : IPlatformUserSalonEntityService
{
    private readonly AppDbContext _db;

    public PlatformUserSalonEntityService(AppDbContext db) => _db = db;

    public IQueryable<PlatformUserSalon> GetAllQueryable()
        => _db.PlatformUserSalons.AsQueryable();

    public Task<PlatformUserSalon?> GetByIdAsync(int id)
        => _db.PlatformUserSalons.FindAsync(id).AsTask();

    public void Add(PlatformUserSalon entity) => _db.PlatformUserSalons.Add(entity);
    public void Update(PlatformUserSalon entity) => _db.PlatformUserSalons.Update(entity);
    public void Remove(PlatformUserSalon entity) => _db.PlatformUserSalons.Remove(entity);
}
