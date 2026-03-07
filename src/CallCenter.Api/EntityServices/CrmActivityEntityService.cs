using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CrmActivityEntityService : ICrmActivityEntityService
{
    private readonly AppDbContext _db;

    public CrmActivityEntityService(AppDbContext db) => _db = db;

    public IQueryable<CrmActivity> GetAllQueryable()
        => _db.CrmActivities.AsQueryable();

    public Task<CrmActivity?> GetByIdAsync(int id)
        => _db.CrmActivities.FindAsync(id).AsTask();

    public void Add(CrmActivity entity) => _db.CrmActivities.Add(entity);
    public void Remove(CrmActivity entity) => _db.CrmActivities.Remove(entity);
}
