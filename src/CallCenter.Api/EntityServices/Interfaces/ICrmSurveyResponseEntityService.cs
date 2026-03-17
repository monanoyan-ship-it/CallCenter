using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmSurveyResponseEntityService
{
    IQueryable<CrmSurveyResponse> GetAllQueryable();
    Task<CrmSurveyResponse?> GetByIdAsync(int id);
    void Add(CrmSurveyResponse entity);
    void Remove(CrmSurveyResponse entity);
}
