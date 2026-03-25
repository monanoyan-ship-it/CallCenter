using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmContactEntityService
{
    IQueryable<CrmContact> GetAllQueryable();
    Task<CrmContact?> GetByIdAsync(int id);
    void Add(CrmContact entity);
    void AddRange(IEnumerable<CrmContact> entities);
    void Update(CrmContact entity);
    void Remove(CrmContact entity);
}
