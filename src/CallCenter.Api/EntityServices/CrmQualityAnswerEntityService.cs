using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CrmQualityAnswerEntityService : ICrmQualityAnswerEntityService
{
    private readonly AppDbContext _db;

    public CrmQualityAnswerEntityService(AppDbContext db) => _db = db;

    public Task<CrmQualityAnswer?> GetByIdAsync(int id)
        => _db.CrmQualityAnswers.FindAsync(id).AsTask();

    public IQueryable<CrmQualityAnswer> GetAllQueryable()
        => _db.CrmQualityAnswers.AsQueryable();

    public void Add(CrmQualityAnswer entity) => _db.CrmQualityAnswers.Add(entity);
    public void AddRange(IEnumerable<CrmQualityAnswer> entities) => _db.CrmQualityAnswers.AddRange(entities);
    public void Update(CrmQualityAnswer entity) => _db.CrmQualityAnswers.Update(entity);
    public void DeleteRange(IEnumerable<CrmQualityAnswer> entities) => _db.CrmQualityAnswers.RemoveRange(entities);
}
