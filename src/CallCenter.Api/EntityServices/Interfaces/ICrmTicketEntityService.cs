using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmTicketEntityService
{
    IQueryable<CrmTicket> GetAllQueryable();
    Task<CrmTicket?> GetByIdAsync(int id);
    void Add(CrmTicket entity);
    void Update(CrmTicket entity);
    void Remove(CrmTicket entity);
}
