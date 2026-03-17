using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmTicketCategoryEntityService
{
    IQueryable<CrmTicketCategory> GetAllQueryable();
    Task<CrmTicketCategory?> GetByIdAsync(int id);
    void Add(CrmTicketCategory entity);
    void Update(CrmTicketCategory entity);
    void Remove(CrmTicketCategory entity);
}
