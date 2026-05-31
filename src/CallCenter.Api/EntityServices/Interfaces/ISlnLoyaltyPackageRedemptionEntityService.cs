using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnLoyaltyPackageRedemptionEntityService
{
    IQueryable<SlnLoyaltyPackageRedemption> GetAllQueryable();
    Task<SlnLoyaltyPackageRedemption?> GetByIdAsync(int id);
    void Add(SlnLoyaltyPackageRedemption entity);
    void Update(SlnLoyaltyPackageRedemption entity);
    void Remove(SlnLoyaltyPackageRedemption entity);
}
