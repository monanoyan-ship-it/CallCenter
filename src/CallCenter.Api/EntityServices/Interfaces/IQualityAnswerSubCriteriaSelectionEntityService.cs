using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IQualityAnswerSubCriteriaSelectionEntityService
{
    IQueryable<QualityAnswerSubCriteriaSelection> GetAllQueryable();
    void AddRange(IEnumerable<QualityAnswerSubCriteriaSelection> entities);
    void DeleteRange(IEnumerable<QualityAnswerSubCriteriaSelection> entities);
}
