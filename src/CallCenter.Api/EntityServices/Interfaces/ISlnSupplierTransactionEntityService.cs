using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnSupplierTransactionEntityService
{
    IQueryable<SlnSupplierTransaction> GetAllQueryable();
    Task<SlnSupplierTransaction?> GetByIdAsync(int id);
    void Add(SlnSupplierTransaction entity);
    void Remove(SlnSupplierTransaction entity);
}
