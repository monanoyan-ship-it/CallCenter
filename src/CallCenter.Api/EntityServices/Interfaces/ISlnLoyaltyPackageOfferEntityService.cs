using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnLoyaltyPackageOfferEntityService
{
    IQueryable<SlnLoyaltyPackageOffer> GetAllQueryable();
    Task<SlnLoyaltyPackageOffer?> GetByIdAsync(int id);
    void Add(SlnLoyaltyPackageOffer entity);
    void Update(SlnLoyaltyPackageOffer entity);
    void Remove(SlnLoyaltyPackageOffer entity);
}
