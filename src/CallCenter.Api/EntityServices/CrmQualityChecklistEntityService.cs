using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class CrmQualityChecklistEntityService : ICrmQualityChecklistEntityService
{
    private readonly AppDbContext _db;

    public CrmQualityChecklistEntityService(AppDbContext db) => _db = db;

    public Task<CrmQualityChecklist?> GetByIdAsync(int id)
        => _db.CrmQualityChecklists.FindAsync(id).AsTask();

    public Task<CrmQualityChecklist?> GetByUidAsync(Guid uid)
        => _db.CrmQualityChecklists.FirstOrDefaultAsync(c => c.Uid == uid);

    public IQueryable<CrmQualityChecklist> GetAllQueryable()
        => _db.CrmQualityChecklists.AsQueryable();

    public void Add(CrmQualityChecklist entity) => _db.CrmQualityChecklists.Add(entity);
    public void Update(CrmQualityChecklist entity) => _db.CrmQualityChecklists.Update(entity);
    public void Delete(CrmQualityChecklist entity) => _db.CrmQualityChecklists.Remove(entity);
}
