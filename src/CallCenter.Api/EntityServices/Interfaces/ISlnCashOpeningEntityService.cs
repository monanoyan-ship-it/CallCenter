using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnCashOpeningEntityService
{
    IQueryable<SlnCashOpening> GetAllQueryable();
    Task<SlnCashOpening?> GetByIdAsync(int id);
    void Add(SlnCashOpening entity);
    void Update(SlnCashOpening entity);
    void Remove(SlnCashOpening entity);
}
