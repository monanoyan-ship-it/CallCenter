using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnLoyaltyProgramRewardEntityService
{
    IQueryable<SlnLoyaltyProgramReward> GetAllQueryable();
    Task<SlnLoyaltyProgramReward?> GetByIdAsync(int id);
    void Add(SlnLoyaltyProgramReward entity);
    void Update(SlnLoyaltyProgramReward entity);
    void Remove(SlnLoyaltyProgramReward entity);
}
