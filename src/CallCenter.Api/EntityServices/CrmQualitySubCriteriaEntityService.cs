using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CrmQualitySubCriteriaEntityService : ICrmQualitySubCriteriaEntityService
{
    private readonly AppDbContext _db;

    public CrmQualitySubCriteriaEntityService(AppDbContext db) => _db = db;

    public Task<CrmQualityQuestionSubCriteria?> GetByIdAsync(int id)
        => _db.CrmQualityQuestionSubCriteria.FindAsync(id).AsTask();

    public IQueryable<CrmQualityQuestionSubCriteria> GetAllQueryable()
        => _db.CrmQualityQuestionSubCriteria.AsQueryable();

    public void Add(CrmQualityQuestionSubCriteria entity) => _db.CrmQualityQuestionSubCriteria.Add(entity);
    public void AddRange(IEnumerable<CrmQualityQuestionSubCriteria> entities) => _db.CrmQualityQuestionSubCriteria.AddRange(entities);
    public void Delete(CrmQualityQuestionSubCriteria entity) => _db.CrmQualityQuestionSubCriteria.Remove(entity);
    public void DeleteRange(IEnumerable<CrmQualityQuestionSubCriteria> entities) => _db.CrmQualityQuestionSubCriteria.RemoveRange(entities);
}
