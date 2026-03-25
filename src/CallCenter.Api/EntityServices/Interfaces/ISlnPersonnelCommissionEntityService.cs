using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnPersonnelCommissionEntityService
{
    IQueryable<SlnPersonnelCommission> GetAllQueryable();
    Task<SlnPersonnelCommission?> GetByIdAsync(int id);
    void Add(SlnPersonnelCommission entity);
    void Update(SlnPersonnelCommission entity);
    void Remove(SlnPersonnelCommission entity);
}
