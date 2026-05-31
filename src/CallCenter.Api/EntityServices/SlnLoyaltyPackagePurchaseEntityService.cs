using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnLoyaltyPackagePurchaseEntityService : ISlnLoyaltyPackagePurchaseEntityService
{
    private readonly AppDbContext _db;

    public SlnLoyaltyPackagePurchaseEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnLoyaltyPackagePurchase> GetAllQueryable()
        => _db.SlnLoyaltyPackagePurchases.AsQueryable();

    public Task<SlnLoyaltyPackagePurchase?> GetByIdAsync(int id)
        => _db.SlnLoyaltyPackagePurchases.FindAsync(id).AsTask();

    public void Add(SlnLoyaltyPackagePurchase entity) => _db.SlnLoyaltyPackagePurchases.Add(entity);
    public void Update(SlnLoyaltyPackagePurchase entity) => _db.SlnLoyaltyPackagePurchases.Update(entity);
    public void Remove(SlnLoyaltyPackagePurchase entity) => _db.SlnLoyaltyPackagePurchases.Remove(entity);
}
