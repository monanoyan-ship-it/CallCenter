using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CrmContactTagEntityService : ICrmContactTagEntityService
{
    private readonly AppDbContext _db;

    public CrmContactTagEntityService(AppDbContext db) => _db = db;

    public IQueryable<CrmContactTag> GetAllQueryable()
        => _db.CrmContactTags.AsQueryable();

    public Task<CrmContactTag?> GetByIdAsync(int id)
        => _db.CrmContactTags.FindAsync(id).AsTask();

    public void Add(CrmContactTag entity) => _db.CrmContactTags.Add(entity);
    public void Remove(CrmContactTag entity) => _db.CrmContactTags.Remove(entity);
}
