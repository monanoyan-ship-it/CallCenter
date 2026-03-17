using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmSurveyEntityService
{
    IQueryable<CrmSurvey> GetAllQueryable();
    Task<CrmSurvey?> GetByIdAsync(int id);
    void Add(CrmSurvey entity);
    void Update(CrmSurvey entity);
    void Remove(CrmSurvey entity);
}
