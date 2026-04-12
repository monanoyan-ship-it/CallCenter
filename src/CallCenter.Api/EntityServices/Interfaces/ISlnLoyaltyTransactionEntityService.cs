using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnLoyaltyTransactionEntityService
{
    IQueryable<SlnLoyaltyTransaction> GetAllQueryable();
    Task<SlnLoyaltyTransaction?> GetByIdAsync(int id);
    void Add(SlnLoyaltyTransaction entity);
    void Update(SlnLoyaltyTransaction entity);
    void Remove(SlnLoyaltyTransaction entity);
}
