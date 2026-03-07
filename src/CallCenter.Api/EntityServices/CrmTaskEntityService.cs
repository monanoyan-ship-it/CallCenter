using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CrmTaskEntityService : ICrmTaskEntityService
{
    private readonly AppDbContext _db;

    public CrmTaskEntityService(AppDbContext db) => _db = db;

    public IQueryable<CrmTask> GetAllQueryable()
        => _db.CrmTasks.AsQueryable();

    public Task<CrmTask?> GetByIdAsync(int id)
        => _db.CrmTasks.FindAsync(id).AsTask();

    public void Add(CrmTask entity) => _db.CrmTasks.Add(entity);
    public void Update(CrmTask entity) => _db.CrmTasks.Update(entity);
    public void Remove(CrmTask entity) => _db.CrmTasks.Remove(entity);
}
