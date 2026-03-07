using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CrmTicketEntityService : ICrmTicketEntityService
{
    private readonly AppDbContext _db;

    public CrmTicketEntityService(AppDbContext db) => _db = db;

    public IQueryable<CrmTicket> GetAllQueryable()
        => _db.CrmTickets.AsQueryable();

    public Task<CrmTicket?> GetByIdAsync(int id)
        => _db.CrmTickets.FindAsync(id).AsTask();

    public void Add(CrmTicket entity) => _db.CrmTickets.Add(entity);
    public void Update(CrmTicket entity) => _db.CrmTickets.Update(entity);
    public void Remove(CrmTicket entity) => _db.CrmTickets.Remove(entity);
}
