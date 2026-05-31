using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnClientLoyaltyProgressEntityService
{
    IQueryable<SlnClientLoyaltyProgress> GetAllQueryable();
    Task<SlnClientLoyaltyProgress?> GetByIdAsync(int id);
    void Add(SlnClientLoyaltyProgress entity);
    void Update(SlnClientLoyaltyProgress entity);
    void Remove(SlnClientLoyaltyProgress entity);
}
