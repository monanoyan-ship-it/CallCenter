using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICustomerPortalModuleEntityService
{
    IQueryable<CustomerPortalModule> GetAllQueryable();
    Task<CustomerPortalModule?> GetByCustomerAndModuleAsync(int customerId, int moduleId);
    Task<List<int>> GetActiveModuleIdsAsync(int customerId);
    void Add(CustomerPortalModule entity);
    void Remove(CustomerPortalModule entity);
}
