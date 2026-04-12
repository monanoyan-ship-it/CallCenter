using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnMembershipUsageEntityService
{
    IQueryable<SlnMembershipUsage> GetAllQueryable();
    Task<SlnMembershipUsage?> GetByIdAsync(int id);
    void Add(SlnMembershipUsage entity);
    void Update(SlnMembershipUsage entity);
    void Remove(SlnMembershipUsage entity);
}
