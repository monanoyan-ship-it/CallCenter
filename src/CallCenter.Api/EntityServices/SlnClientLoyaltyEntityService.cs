using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnClientLoyaltyEntityService : ISlnClientLoyaltyEntityService
{
    private readonly AppDbContext _db;

    public SlnClientLoyaltyEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnClientLoyalty> GetAllQueryable()
        => _db.SlnClientLoyalties.AsQueryable();

    public Task<SlnClientLoyalty?> GetByIdAsync(int id)
        => _db.SlnClientLoyalties.FindAsync(id).AsTask();

    public void Add(SlnClientLoyalty entity) => _db.SlnClientLoyalties.Add(entity);
    public void Update(SlnClientLoyalty entity) => _db.SlnClientLoyalties.Update(entity);
    public void Remove(SlnClientLoyalty entity) => _db.SlnClientLoyalties.Remove(entity);
}
