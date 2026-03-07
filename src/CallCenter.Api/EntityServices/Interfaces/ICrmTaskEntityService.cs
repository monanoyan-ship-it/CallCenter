using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmTaskEntityService
{
    IQueryable<CrmTask> GetAllQueryable();
    Task<CrmTask?> GetByIdAsync(int id);
    void Add(CrmTask entity);
    void Update(CrmTask entity);
    void Remove(CrmTask entity);
}
