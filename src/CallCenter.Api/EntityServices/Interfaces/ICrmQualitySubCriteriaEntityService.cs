using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmQualitySubCriteriaEntityService
{
    Task<CrmQualityQuestionSubCriteria?> GetByIdAsync(int id);
    IQueryable<CrmQualityQuestionSubCriteria> GetAllQueryable();
    void Add(CrmQualityQuestionSubCriteria entity);
    void AddRange(IEnumerable<CrmQualityQuestionSubCriteria> entities);
    void Delete(CrmQualityQuestionSubCriteria entity);
    void DeleteRange(IEnumerable<CrmQualityQuestionSubCriteria> entities);
}
