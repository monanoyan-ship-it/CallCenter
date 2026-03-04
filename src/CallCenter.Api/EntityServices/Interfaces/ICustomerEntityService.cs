using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICustomerEntityService
{
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer?> GetByIdWithPersonnelAsync(int id);
    Task<Customer?> GetByIdWithPortalModulesAsync(int id);
    IQueryable<Customer> GetAllQueryable();
    void Add(Customer entity);
    void Update(Customer entity);
    void Remove(Customer entity);
}
