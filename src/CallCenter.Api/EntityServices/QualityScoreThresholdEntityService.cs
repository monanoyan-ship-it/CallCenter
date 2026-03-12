using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class QualityScoreThresholdEntityService : IQualityScoreThresholdEntityService
{
    private readonly AppDbContext _db;

    public QualityScoreThresholdEntityService(AppDbContext db) => _db = db;

    public Task<QualityScoreThreshold?> GetByIdAsync(int id)
        => _db.QualityScoreThresholds.FindAsync(id).AsTask();

    public IQueryable<QualityScoreThreshold> GetAllQueryable()
        => _db.QualityScoreThresholds.AsQueryable();

    public void Add(QualityScoreThreshold entity) => _db.QualityScoreThresholds.Add(entity);
    public void Update(QualityScoreThreshold entity) => _db.QualityScoreThresholds.Update(entity);
    public void Delete(QualityScoreThreshold entity) => _db.QualityScoreThresholds.Remove(entity);
}
