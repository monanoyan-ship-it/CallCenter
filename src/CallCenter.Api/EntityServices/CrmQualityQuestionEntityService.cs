using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CrmQualityQuestionEntityService : ICrmQualityQuestionEntityService
{
    private readonly AppDbContext _db;

    public CrmQualityQuestionEntityService(AppDbContext db) => _db = db;

    public Task<CrmQualityQuestion?> GetByIdAsync(int id)
        => _db.CrmQualityQuestions.FindAsync(id).AsTask();

    public IQueryable<CrmQualityQuestion> GetAllQueryable()
        => _db.CrmQualityQuestions.AsQueryable();

    public void Add(CrmQualityQuestion entity) => _db.CrmQualityQuestions.Add(entity);
    public void AddRange(IEnumerable<CrmQualityQuestion> entities) => _db.CrmQualityQuestions.AddRange(entities);
    public void Update(CrmQualityQuestion entity) => _db.CrmQualityQuestions.Update(entity);
    public void Delete(CrmQualityQuestion entity) => _db.CrmQualityQuestions.Remove(entity);
}
