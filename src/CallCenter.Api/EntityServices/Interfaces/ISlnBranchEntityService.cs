using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnBranchEntityService
{
    IQueryable<SlnBranch> GetAllQueryable();
    Task<SlnBranch?> GetByIdAsync(int id);
    void Add(SlnBranch entity);
    void Update(SlnBranch entity);
    void Remove(SlnBranch entity);
}
