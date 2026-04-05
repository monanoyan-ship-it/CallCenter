using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICustomerEmailIntegrationEntityService
{
    Task<CustomerEmailIntegration?> GetByIdAsync(int id);
    Task<CustomerEmailIntegration?> GetByUidAsync(Guid uid);
    IQueryable<CustomerEmailIntegration> GetByCustomerQueryable(int customerId);
    Task<CustomerEmailIntegration?> GetDefaultAsync(int customerId);
    void Add(CustomerEmailIntegration entity);
    void Update(CustomerEmailIntegration entity);
    void Remove(CustomerEmailIntegration entity);
}
