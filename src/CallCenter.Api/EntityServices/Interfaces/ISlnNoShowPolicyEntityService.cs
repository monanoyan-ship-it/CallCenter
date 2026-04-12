using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnNoShowPolicyEntityService
{
    IQueryable<SlnNoShowPolicy> GetAllQueryable();
    Task<SlnNoShowPolicy?> GetByIdAsync(int id);
    void Add(SlnNoShowPolicy entity);
    void Update(SlnNoShowPolicy entity);
    void Remove(SlnNoShowPolicy entity);
}
