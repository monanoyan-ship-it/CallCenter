using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnClientLedgerEntityService
{
    IQueryable<SlnClientLedger> GetAllQueryable();
    Task<SlnClientLedger?> GetByIdAsync(int id);
    void Add(SlnClientLedger entity);
    void Update(SlnClientLedger entity);
    void Remove(SlnClientLedger entity);
}
