using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnResourceEntityService : ISlnResourceEntityService
{
    private readonly AppDbContext _db;

    public SlnResourceEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnResource> GetAllQueryable()
        => _db.SlnResources.AsQueryable();

    public Task<SlnResource?> GetByIdAsync(int id)
        => _db.SlnResources.FindAsync(id).AsTask();

    public void Add(SlnResource entity) => _db.SlnResources.Add(entity);
    public void Update(SlnResource entity) => _db.SlnResources.Update(entity);
    public void Remove(SlnResource entity) => _db.SlnResources.Remove(entity);
}
