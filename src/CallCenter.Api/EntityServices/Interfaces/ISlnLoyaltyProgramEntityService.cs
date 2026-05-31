using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnLoyaltyProgramEntityService
{
    IQueryable<SlnLoyaltyProgram> GetAllQueryable();
    Task<SlnLoyaltyProgram?> GetByIdAsync(int id);
    void Add(SlnLoyaltyProgram entity);
    void Update(SlnLoyaltyProgram entity);
    void Remove(SlnLoyaltyProgram entity);
}
