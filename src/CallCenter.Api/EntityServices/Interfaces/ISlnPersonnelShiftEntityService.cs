using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnPersonnelShiftEntityService
{
    IQueryable<SlnPersonnelShift> GetAllQueryable();
    Task<SlnPersonnelShift?> GetByIdAsync(int id);
    void Add(SlnPersonnelShift entity);
    void Update(SlnPersonnelShift entity);
    void Remove(SlnPersonnelShift entity);
}
