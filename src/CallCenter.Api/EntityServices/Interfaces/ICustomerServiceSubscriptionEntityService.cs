using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICustomerServiceSubscriptionEntityService
{
    Task<CustomerServiceSubscription?> GetByIdAsync(int id);
    Task<CustomerServiceSubscription?> GetByUidAsync(Guid uid);
    IQueryable<CustomerServiceSubscription> GetAllQueryable();
    void Add(CustomerServiceSubscription entity);
    void Update(CustomerServiceSubscription entity);
}
