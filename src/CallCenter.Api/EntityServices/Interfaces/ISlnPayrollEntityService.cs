using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnPayrollEntityService
{
    IQueryable<SlnPayroll> GetAllQueryable();
    Task<SlnPayroll?> GetByIdAsync(int id);
    void Add(SlnPayroll entity);
    void Update(SlnPayroll entity);
    void Remove(SlnPayroll entity);
}
