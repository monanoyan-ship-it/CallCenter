using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CrmTicketCommentEntityService : ICrmTicketCommentEntityService
{
    private readonly AppDbContext _db;

    public CrmTicketCommentEntityService(AppDbContext db) => _db = db;

    public IQueryable<CrmTicketComment> GetAllQueryable()
        => _db.CrmTicketComments.AsQueryable();

    public Task<CrmTicketComment?> GetByIdAsync(int id)
        => _db.CrmTicketComments.FindAsync(id).AsTask();

    public void Add(CrmTicketComment entity) => _db.CrmTicketComments.Add(entity);
    public void Remove(CrmTicketComment entity) => _db.CrmTicketComments.Remove(entity);
}
