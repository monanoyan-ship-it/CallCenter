using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class QualitySubCriteriaEntityService : IQualitySubCriteriaEntityService
{
    private readonly AppDbContext _db;

    public QualitySubCriteriaEntityService(AppDbContext db) => _db = db;

    public Task<QualityQuestionSubCriteria?> GetByIdAsync(int id)
        => _db.QualityQuestionSubCriteria.FindAsync(id).AsTask();

    public IQueryable<QualityQuestionSubCriteria> GetAllQueryable()
        => _db.QualityQuestionSubCriteria.AsQueryable();

    public void Add(QualityQuestionSubCriteria entity) => _db.QualityQuestionSubCriteria.Add(entity);
    public void AddRange(IEnumerable<QualityQuestionSubCriteria> entities) => _db.QualityQuestionSubCriteria.AddRange(entities);
    public void Delete(QualityQuestionSubCriteria entity) => _db.QualityQuestionSubCriteria.Remove(entity);
    public void DeleteRange(IEnumerable<QualityQuestionSubCriteria> entities) => _db.QualityQuestionSubCriteria.RemoveRange(entities);
}
