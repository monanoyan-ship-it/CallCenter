using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnLoyaltyPackageRedemptionEntityService : ISlnLoyaltyPackageRedemptionEntityService
{
    private readonly AppDbContext _db;

    public SlnLoyaltyPackageRedemptionEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnLoyaltyPackageRedemption> GetAllQueryable()
        => _db.SlnLoyaltyPackageRedemptions.AsQueryable();

    public Task<SlnLoyaltyPackageRedemption?> GetByIdAsync(int id)
        => _db.SlnLoyaltyPackageRedemptions.FindAsync(id).AsTask();

    public void Add(SlnLoyaltyPackageRedemption entity) => _db.SlnLoyaltyPackageRedemptions.Add(entity);
    public void Update(SlnLoyaltyPackageRedemption entity) => _db.SlnLoyaltyPackageRedemptions.Update(entity);
    public void Remove(SlnLoyaltyPackageRedemption entity) => _db.SlnLoyaltyPackageRedemptions.Remove(entity);
}
