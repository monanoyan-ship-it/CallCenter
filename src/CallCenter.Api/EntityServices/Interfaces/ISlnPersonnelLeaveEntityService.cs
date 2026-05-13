using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnPersonnelLeaveEntityService
{
    IQueryable<SlnPersonnelLeave> GetAllQueryable();
    Task<SlnPersonnelLeave?> GetByIdAsync(int id);
    void Add(SlnPersonnelLeave entity);
    void Update(SlnPersonnelLeave entity);
    void Remove(SlnPersonnelLeave entity);
}
