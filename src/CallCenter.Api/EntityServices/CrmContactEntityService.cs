using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CrmContactEntityService : ICrmContactEntityService
{
    private readonly AppDbContext _db;

    public CrmContactEntityService(AppDbContext db) => _db = db;

    public IQueryable<CrmContact> GetAllQueryable()
        => _db.CrmContacts.AsQueryable();

    public Task<CrmContact?> GetByIdAsync(int id)
        => _db.CrmContacts.FindAsync(id).AsTask();

    public void Add(CrmContact entity) => _db.CrmContacts.Add(entity);
    public void AddRange(IEnumerable<CrmContact> entities) => _db.CrmContacts.AddRange(entities);
    public void Update(CrmContact entity) => _db.CrmContacts.Update(entity);
    public void Remove(CrmContact entity) => _db.CrmContacts.Remove(entity);
}
