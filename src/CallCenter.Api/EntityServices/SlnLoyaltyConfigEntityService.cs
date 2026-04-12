using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnLoyaltyConfigEntityService : ISlnLoyaltyConfigEntityService
{
    private readonly AppDbContext _db;

    public SlnLoyaltyConfigEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnLoyaltyConfig> GetAllQueryable()
        => _db.SlnLoyaltyConfigs.AsQueryable();

    public Task<SlnLoyaltyConfig?> GetByIdAsync(int id)
        => _db.SlnLoyaltyConfigs.FindAsync(id).AsTask();

    public void Add(SlnLoyaltyConfig entity) => _db.SlnLoyaltyConfigs.Add(entity);
    public void Update(SlnLoyaltyConfig entity) => _db.SlnLoyaltyConfigs.Update(entity);
    public void Remove(SlnLoyaltyConfig entity) => _db.SlnLoyaltyConfigs.Remove(entity);
}
