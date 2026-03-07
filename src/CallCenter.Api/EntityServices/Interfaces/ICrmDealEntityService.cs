using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmDealEntityService
{
    IQueryable<CrmDeal> GetAllQueryable();
    Task<CrmDeal?> GetByIdAsync(int id);
    void Add(CrmDeal entity);
    void Update(CrmDeal entity);
    void Remove(CrmDeal entity);
}
