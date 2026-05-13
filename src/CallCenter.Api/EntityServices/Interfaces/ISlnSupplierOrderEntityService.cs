using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnSupplierOrderEntityService
{
    IQueryable<SlnSupplierOrder> GetAllQueryable();
    Task<SlnSupplierOrder?> GetByIdAsync(int id);
    void Add(SlnSupplierOrder entity);
    void Update(SlnSupplierOrder entity);
    void Remove(SlnSupplierOrder entity);
}
