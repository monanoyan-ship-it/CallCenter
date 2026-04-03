using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICustomerProductEntityService
{
    Task<CustomerProduct?> GetByIdAsync(int id);
    Task<List<CustomerProduct>> GetByCustomerIdAsync(int customerId);
    IQueryable<CustomerProduct> GetAllQueryable();
    void Add(CustomerProduct entity);
    void Update(CustomerProduct entity);
    void Remove(CustomerProduct entity);
}
