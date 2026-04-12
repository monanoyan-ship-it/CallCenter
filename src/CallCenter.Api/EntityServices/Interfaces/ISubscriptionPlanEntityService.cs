using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISubscriptionPlanEntityService
{
    IQueryable<SubscriptionPlan> GetAllQueryable();
    Task<SubscriptionPlan?> GetByIdAsync(int id);
    void Add(SubscriptionPlan entity);
    void Update(SubscriptionPlan entity);
    void Remove(SubscriptionPlan entity);
}
