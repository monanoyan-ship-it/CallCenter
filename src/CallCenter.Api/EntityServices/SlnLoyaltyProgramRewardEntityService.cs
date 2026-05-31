using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnLoyaltyProgramRewardEntityService : ISlnLoyaltyProgramRewardEntityService
{
    private readonly AppDbContext _db;

    public SlnLoyaltyProgramRewardEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnLoyaltyProgramReward> GetAllQueryable() => _db.SlnLoyaltyProgramRewards.AsQueryable();
    public Task<SlnLoyaltyProgramReward?> GetByIdAsync(int id) => _db.SlnLoyaltyProgramRewards.FindAsync(id).AsTask();
    public void Add(SlnLoyaltyProgramReward entity) => _db.SlnLoyaltyProgramRewards.Add(entity);
    public void Update(SlnLoyaltyProgramReward entity) => _db.SlnLoyaltyProgramRewards.Update(entity);
    public void Remove(SlnLoyaltyProgramReward entity) => _db.SlnLoyaltyProgramRewards.Remove(entity);
}
