using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class QualityChecklistEntityService : IQualityChecklistEntityService
{
    private readonly AppDbContext _db;

    public QualityChecklistEntityService(AppDbContext db) => _db = db;

    public Task<QualityChecklist?> GetByIdAsync(int id)
        => _db.QualityChecklists.FindAsync(id).AsTask();

    public Task<QualityChecklist?> GetByUidAsync(Guid uid)
        => _db.QualityChecklists.FirstOrDefaultAsync(c => c.Uid == uid);

    public IQueryable<QualityChecklist> GetAllQueryable()
        => _db.QualityChecklists.AsQueryable();

    public void Add(QualityChecklist entity) => _db.QualityChecklists.Add(entity);
    public void Update(QualityChecklist entity) => _db.QualityChecklists.Update(entity);
    public void Delete(QualityChecklist entity) => _db.QualityChecklists.Remove(entity);
}
