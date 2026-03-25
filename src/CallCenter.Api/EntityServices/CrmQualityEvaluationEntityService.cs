using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class CrmQualityEvaluationEntityService : ICrmQualityEvaluationEntityService
{
    private readonly AppDbContext _db;

    public CrmQualityEvaluationEntityService(AppDbContext db) => _db = db;

    public Task<CrmQualityEvaluation?> GetByIdAsync(int id)
        => _db.CrmQualityEvaluations.FindAsync(id).AsTask();

    public Task<CrmQualityEvaluation?> GetByUidAsync(Guid uid)
        => _db.CrmQualityEvaluations.FirstOrDefaultAsync(e => e.Uid == uid);

    public IQueryable<CrmQualityEvaluation> GetAllQueryable()
        => _db.CrmQualityEvaluations.AsQueryable();

    public void Add(CrmQualityEvaluation entity) => _db.CrmQualityEvaluations.Add(entity);
    public void Update(CrmQualityEvaluation entity) => _db.CrmQualityEvaluations.Update(entity);
}
