using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnServiceSessionPlanEntityService
{
    IQueryable<SlnServiceSessionPlan> GetAllQueryable();
    Task<SlnServiceSessionPlan?> GetByIdAsync(int id);
    void Add(SlnServiceSessionPlan entity);
    void Update(SlnServiceSessionPlan entity);
    void Remove(SlnServiceSessionPlan entity);
}
