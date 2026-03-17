using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmTicketCommentEntityService
{
    IQueryable<CrmTicketComment> GetAllQueryable();
    Task<CrmTicketComment?> GetByIdAsync(int id);
    void Add(CrmTicketComment entity);
    void Remove(CrmTicketComment entity);
}
