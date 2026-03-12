using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IQualityEvaluationEntityService
{
    Task<QualityEvaluation?> GetByIdAsync(int id);
    Task<QualityEvaluation?> GetByUidAsync(Guid uid);
    IQueryable<QualityEvaluation> GetAllQueryable();
    void Add(QualityEvaluation entity);
    void Update(QualityEvaluation entity);
}
