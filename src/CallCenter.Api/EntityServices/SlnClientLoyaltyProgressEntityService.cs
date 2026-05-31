using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnClientLoyaltyProgressEntityService : ISlnClientLoyaltyProgressEntityService
{
    private readonly AppDbContext _db;

    public SlnClientLoyaltyProgressEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnClientLoyaltyProgress> GetAllQueryable() => _db.SlnClientLoyaltyProgresses.AsQueryable();
    public Task<SlnClientLoyaltyProgress?> GetByIdAsync(int id) => _db.SlnClientLoyaltyProgresses.FindAsync(id).AsTask();
    public void Add(SlnClientLoyaltyProgress entity) => _db.SlnClientLoyaltyProgresses.Add(entity);
    public void Update(SlnClientLoyaltyProgress entity) => _db.SlnClientLoyaltyProgresses.Update(entity);
    public void Remove(SlnClientLoyaltyProgress entity) => _db.SlnClientLoyaltyProgresses.Remove(entity);
}
