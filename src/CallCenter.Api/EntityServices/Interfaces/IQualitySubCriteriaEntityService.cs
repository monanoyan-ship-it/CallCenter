using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IQualitySubCriteriaEntityService
{
    Task<QualityQuestionSubCriteria?> GetByIdAsync(int id);
    IQueryable<QualityQuestionSubCriteria> GetAllQueryable();
    void Add(QualityQuestionSubCriteria entity);
    void AddRange(IEnumerable<QualityQuestionSubCriteria> entities);
    void Delete(QualityQuestionSubCriteria entity);
    void DeleteRange(IEnumerable<QualityQuestionSubCriteria> entities);
}
