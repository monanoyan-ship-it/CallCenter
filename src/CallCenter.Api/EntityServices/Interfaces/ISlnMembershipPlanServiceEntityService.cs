using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnMembershipPlanServiceEntityService
{
    IQueryable<SlnMembershipPlanService> GetAllQueryable();
    Task<SlnMembershipPlanService?> GetByIdAsync(int id);
    void Add(SlnMembershipPlanService entity);
    void Update(SlnMembershipPlanService entity);
    void Remove(SlnMembershipPlanService entity);
    void RemoveRange(IEnumerable<SlnMembershipPlanService> entities);
}
