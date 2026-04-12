using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnCashClosingEntityService
{
    IQueryable<SlnCashClosing> GetAllQueryable();
    Task<SlnCashClosing?> GetByIdAsync(int id);
    void Add(SlnCashClosing entity);
    void Update(SlnCashClosing entity);
    void Remove(SlnCashClosing entity);
}
