using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class QualityAnswerEntityService : IQualityAnswerEntityService
{
    private readonly AppDbContext _db;

    public QualityAnswerEntityService(AppDbContext db) => _db = db;

    public Task<QualityAnswer?> GetByIdAsync(int id)
        => _db.QualityAnswers.FindAsync(id).AsTask();

    public IQueryable<QualityAnswer> GetAllQueryable()
        => _db.QualityAnswers.AsQueryable();

    public void Add(QualityAnswer entity) => _db.QualityAnswers.Add(entity);
    public void AddRange(IEnumerable<QualityAnswer> entities) => _db.QualityAnswers.AddRange(entities);
    public void Update(QualityAnswer entity) => _db.QualityAnswers.Update(entity);
    public void DeleteRange(IEnumerable<QualityAnswer> entities) => _db.QualityAnswers.RemoveRange(entities);
}
