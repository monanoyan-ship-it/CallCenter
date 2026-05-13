using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnPersonnelTimesheetEntityService
{
    IQueryable<SlnPersonnelTimesheet> GetAllQueryable();
    Task<SlnPersonnelTimesheet?> GetByIdAsync(int id);
    void Add(SlnPersonnelTimesheet entity);
    void Update(SlnPersonnelTimesheet entity);
    void Remove(SlnPersonnelTimesheet entity);
}
