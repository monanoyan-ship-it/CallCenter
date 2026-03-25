using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnCashTransactionEntityService
{
    IQueryable<SlnCashTransaction> GetAllQueryable();
    Task<SlnCashTransaction?> GetByIdAsync(int id);
    void Add(SlnCashTransaction entity);
    void Remove(SlnCashTransaction entity);
}
