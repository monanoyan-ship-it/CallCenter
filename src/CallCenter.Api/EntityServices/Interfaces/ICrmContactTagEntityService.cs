using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmContactTagEntityService
{
    IQueryable<CrmContactTag> GetAllQueryable();
    Task<CrmContactTag?> GetByIdAsync(int id);
    void Add(CrmContactTag entity);
    void Remove(CrmContactTag entity);
}
