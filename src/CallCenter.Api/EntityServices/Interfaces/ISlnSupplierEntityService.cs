using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnSupplierEntityService
{
    IQueryable<SlnSupplier> GetAllQueryable();
    Task<SlnSupplier?> GetByIdAsync(int id);
    void Add(SlnSupplier entity);
    void Update(SlnSupplier entity);
    void Remove(SlnSupplier entity);
}
