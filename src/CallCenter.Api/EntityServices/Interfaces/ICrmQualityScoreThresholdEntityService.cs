using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmQualityScoreThresholdEntityService
{
    Task<CrmQualityScoreThreshold?> GetByIdAsync(int id);
    IQueryable<CrmQualityScoreThreshold> GetAllQueryable();
    void Add(CrmQualityScoreThreshold entity);
    void Update(CrmQualityScoreThreshold entity);
    void Delete(CrmQualityScoreThreshold entity);
}
