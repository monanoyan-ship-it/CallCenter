using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmActivityEntityService
{
    IQueryable<CrmActivity> GetAllQueryable();
    Task<CrmActivity?> GetByIdAsync(int id);
    void Add(CrmActivity entity);
    void Remove(CrmActivity entity);
}
