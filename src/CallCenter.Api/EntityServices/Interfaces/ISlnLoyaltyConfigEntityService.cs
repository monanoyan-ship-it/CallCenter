using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnLoyaltyConfigEntityService
{
    IQueryable<SlnLoyaltyConfig> GetAllQueryable();
    Task<SlnLoyaltyConfig?> GetByIdAsync(int id);
    void Add(SlnLoyaltyConfig entity);
    void Update(SlnLoyaltyConfig entity);
    void Remove(SlnLoyaltyConfig entity);
}
