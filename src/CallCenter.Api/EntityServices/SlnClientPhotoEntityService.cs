using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnClientPhotoEntityService : ISlnClientPhotoEntityService
{
    private readonly AppDbContext _db;

    public SlnClientPhotoEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnClientPhoto> GetAllQueryable()
        => _db.SlnClientPhotos.AsQueryable();

    public Task<SlnClientPhoto?> GetByIdAsync(int id)
        => _db.SlnClientPhotos.FindAsync(id).AsTask();

    public void Add(SlnClientPhoto entity) => _db.SlnClientPhotos.Add(entity);
    public void Remove(SlnClientPhoto entity) => _db.SlnClientPhotos.Remove(entity);
}
