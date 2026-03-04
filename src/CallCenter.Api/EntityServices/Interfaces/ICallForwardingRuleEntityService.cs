using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICallForwardingRuleEntityService
{
    IQueryable<CallForwardingRule> GetAllQueryable();
    Task<CallForwardingRule?> GetByIdAsync(int id);
    Task<CallForwardingRule?> GetByIdWithUserAsync(int id);
    void Add(CallForwardingRule entity);
    void Remove(CallForwardingRule entity);
}
