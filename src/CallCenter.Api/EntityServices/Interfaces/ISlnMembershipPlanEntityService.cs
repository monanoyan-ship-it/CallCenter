using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnMembershipPlanEntityService
{
    IQueryable<SlnMembershipPlan> GetAllQueryable();
    Task<SlnMembershipPlan?> GetByIdAsync(int id);
    void Add(SlnMembershipPlan entity);
    void Update(SlnMembershipPlan entity);
    void Remove(SlnMembershipPlan entity);
}
