using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnRevenueShareEntityService : ISlnRevenueShareEntityService
{
    private readonly AppDbContext _db;

    public SlnRevenueShareEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnRevenueShare> GetAllQueryable()
        => _db.SlnRevenueShares.AsQueryable();

    public Task<SlnRevenueShare?> GetByIdAsync(int id)
        => _db.SlnRevenueShares.FindAsync(id).AsTask();

    public void Add(SlnRevenueShare entity) => _db.SlnRevenueShares.Add(entity);
    public void Update(SlnRevenueShare entity) => _db.SlnRevenueShares.Update(entity);
    public void Remove(SlnRevenueShare entity) => _db.SlnRevenueShares.Remove(entity);
}
