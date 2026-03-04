using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IBillingPeriodEntityService
{
    Task<CustomerBillingPeriod?> GetByIdAsync(int id);
    IQueryable<CustomerBillingPeriod> GetAllQueryable();
    void Add(CustomerBillingPeriod entity);
    void AddRange(IEnumerable<CustomerBillingPeriod> entities);
    void Update(CustomerBillingPeriod entity);
}
