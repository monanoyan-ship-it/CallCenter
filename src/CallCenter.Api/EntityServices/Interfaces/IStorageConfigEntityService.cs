using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IStorageConfigEntityService
{
    IQueryable<CustomerStorageConfig> GetAllQueryable();
    Task<CustomerStorageConfig?> GetByIdAsync(int id);
    Task<CustomerStorageConfig?> GetDefaultByCustomerAsync(int customerId);
    void Add(CustomerStorageConfig entity);
    void Remove(CustomerStorageConfig entity);
}
