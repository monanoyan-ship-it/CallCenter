using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmQualityEvaluationEntityService
{
    Task<CrmQualityEvaluation?> GetByIdAsync(int id);
    Task<CrmQualityEvaluation?> GetByUidAsync(Guid uid);
    IQueryable<CrmQualityEvaluation> GetAllQueryable();
    void Add(CrmQualityEvaluation entity);
    void Update(CrmQualityEvaluation entity);
}
