using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IQualityScoreThresholdEntityService
{
    Task<QualityScoreThreshold?> GetByIdAsync(int id);
    IQueryable<QualityScoreThreshold> GetAllQueryable();
    void Add(QualityScoreThreshold entity);
    void Update(QualityScoreThreshold entity);
    void Delete(QualityScoreThreshold entity);
}
