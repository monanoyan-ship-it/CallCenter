using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IIntegrationConnectionEntityService
{
    Task<IntegrationConnection?> GetByIdAsync(int id);
    Task<IntegrationConnection?> GetByUidAsync(Guid uid);
    Task<IntegrationConnection?> GetByIdWithDetailsAsync(int id);
    IQueryable<IntegrationConnection> GetAllQueryable();
    IQueryable<IntegrationConnection> GetByCustomerQueryable(int customerId);
    void Add(IntegrationConnection entity);
    void Update(IntegrationConnection entity);
    void Remove(IntegrationConnection entity);
}
