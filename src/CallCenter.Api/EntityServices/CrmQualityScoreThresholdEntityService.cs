using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CrmQualityScoreThresholdEntityService : ICrmQualityScoreThresholdEntityService
{
    private readonly AppDbContext _db;

    public CrmQualityScoreThresholdEntityService(AppDbContext db) => _db = db;

    public Task<CrmQualityScoreThreshold?> GetByIdAsync(int id)
        => _db.CrmQualityScoreThresholds.FindAsync(id).AsTask();

    public IQueryable<CrmQualityScoreThreshold> GetAllQueryable()
        => _db.CrmQualityScoreThresholds.AsQueryable();

    public void Add(CrmQualityScoreThreshold entity) => _db.CrmQualityScoreThresholds.Add(entity);
    public void Update(CrmQualityScoreThreshold entity) => _db.CrmQualityScoreThresholds.Update(entity);
    public void Delete(CrmQualityScoreThreshold entity) => _db.CrmQualityScoreThresholds.Remove(entity);
}
