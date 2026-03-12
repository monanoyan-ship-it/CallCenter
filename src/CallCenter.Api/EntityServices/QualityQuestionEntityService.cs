using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class QualityQuestionEntityService : IQualityQuestionEntityService
{
    private readonly AppDbContext _db;

    public QualityQuestionEntityService(AppDbContext db) => _db = db;

    public Task<QualityQuestion?> GetByIdAsync(int id)
        => _db.QualityQuestions.FindAsync(id).AsTask();

    public IQueryable<QualityQuestion> GetAllQueryable()
        => _db.QualityQuestions.AsQueryable();

    public void Add(QualityQuestion entity) => _db.QualityQuestions.Add(entity);
    public void AddRange(IEnumerable<QualityQuestion> entities) => _db.QualityQuestions.AddRange(entities);
    public void Update(QualityQuestion entity) => _db.QualityQuestions.Update(entity);
    public void Delete(QualityQuestion entity) => _db.QualityQuestions.Remove(entity);
}
