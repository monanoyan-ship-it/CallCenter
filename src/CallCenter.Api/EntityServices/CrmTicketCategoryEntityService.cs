using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CrmTicketCategoryEntityService : ICrmTicketCategoryEntityService
{
    private readonly AppDbContext _db;

    public CrmTicketCategoryEntityService(AppDbContext db) => _db = db;

    public IQueryable<CrmTicketCategory> GetAllQueryable()
        => _db.CrmTicketCategories.AsQueryable();

    public Task<CrmTicketCategory?> GetByIdAsync(int id)
        => _db.CrmTicketCategories.FindAsync(id).AsTask();

    public void Add(CrmTicketCategory entity) => _db.CrmTicketCategories.Add(entity);
    public void Update(CrmTicketCategory entity) => _db.CrmTicketCategories.Update(entity);
    public void Remove(CrmTicketCategory entity) => _db.CrmTicketCategories.Remove(entity);
}
