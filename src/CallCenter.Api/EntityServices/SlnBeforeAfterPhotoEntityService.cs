using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnBeforeAfterPhotoEntityService : ISlnBeforeAfterPhotoEntityService
{
    private readonly AppDbContext _db;

    public SlnBeforeAfterPhotoEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnBeforeAfterPhoto> GetAllQueryable()
        => _db.SlnBeforeAfterPhotos.AsQueryable();

    public Task<SlnBeforeAfterPhoto?> GetByIdAsync(int id)
        => _db.SlnBeforeAfterPhotos.FindAsync(id).AsTask();

    public void Add(SlnBeforeAfterPhoto entity) => _db.SlnBeforeAfterPhotos.Add(entity);
    public void Update(SlnBeforeAfterPhoto entity) => _db.SlnBeforeAfterPhotos.Update(entity);
    public void Remove(SlnBeforeAfterPhoto entity) => _db.SlnBeforeAfterPhotos.Remove(entity);
}
