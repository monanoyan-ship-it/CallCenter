using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IServiceBillingItemEntityService
{
    Task<ServiceBillingItem?> GetByIdAsync(int id);
    IQueryable<ServiceBillingItem> GetAllQueryable();
    void Add(ServiceBillingItem entity);
    void AddRange(IEnumerable<ServiceBillingItem> entities);
    void Update(ServiceBillingItem entity);
}
