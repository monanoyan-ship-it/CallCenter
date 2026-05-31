using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnLoyaltyPackagePurchaseEntityService
{
    IQueryable<SlnLoyaltyPackagePurchase> GetAllQueryable();
    Task<SlnLoyaltyPackagePurchase?> GetByIdAsync(int id);
    void Add(SlnLoyaltyPackagePurchase entity);
    void Update(SlnLoyaltyPackagePurchase entity);
    void Remove(SlnLoyaltyPackagePurchase entity);
}
