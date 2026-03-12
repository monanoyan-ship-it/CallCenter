using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class QualityEvaluationEntityService : IQualityEvaluationEntityService
{
    private readonly AppDbContext _db;

    public QualityEvaluationEntityService(AppDbContext db) => _db = db;

    public Task<QualityEvaluation?> GetByIdAsync(int id)
        => _db.QualityEvaluations.FindAsync(id).AsTask();

    public Task<QualityEvaluation?> GetByUidAsync(Guid uid)
        => _db.QualityEvaluations.FirstOrDefaultAsync(e => e.Uid == uid);

    public IQueryable<QualityEvaluation> GetAllQueryable()
        => _db.QualityEvaluations.AsQueryable();

    public void Add(QualityEvaluation entity) => _db.QualityEvaluations.Add(entity);
    public void Update(QualityEvaluation entity) => _db.QualityEvaluations.Update(entity);
}
