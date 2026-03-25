using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmQualityAnswerSubCriteriaSelectionEntityService
{
    IQueryable<CrmQualityAnswerSubCriteriaSelection> GetAllQueryable();
    void AddRange(IEnumerable<CrmQualityAnswerSubCriteriaSelection> entities);
    void DeleteRange(IEnumerable<CrmQualityAnswerSubCriteriaSelection> entities);
}
