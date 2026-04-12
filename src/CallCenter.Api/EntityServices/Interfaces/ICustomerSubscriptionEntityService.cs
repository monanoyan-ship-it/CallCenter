using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICustomerSubscriptionEntityService
{
    IQueryable<CustomerSubscription> GetAllQueryable();
    Task<CustomerSubscription?> GetByIdAsync(int id);
    void Add(CustomerSubscription entity);
    void Update(CustomerSubscription entity);
    void Remove(CustomerSubscription entity);
}
