using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnSalonProfileEntityService : ISlnSalonProfileEntityService
{
    private readonly AppDbContext _db;

    public SlnSalonProfileEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnSalonProfile> GetAllQueryable()
        => _db.SlnSalonProfiles.AsQueryable();

    public Task<SlnSalonProfile?> GetByIdAsync(int id)
        => _db.SlnSalonProfiles.FindAsync(id).AsTask();

    public void Add(SlnSalonProfile entity) => _db.SlnSalonProfiles.Add(entity);
    public void Update(SlnSalonProfile entity) => _db.SlnSalonProfiles.Update(entity);
    public void Remove(SlnSalonProfile entity) => _db.SlnSalonProfiles.Remove(entity);
}
