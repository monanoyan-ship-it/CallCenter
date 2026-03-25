using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmQualityQuestionEntityService
{
    Task<CrmQualityQuestion?> GetByIdAsync(int id);
    IQueryable<CrmQualityQuestion> GetAllQueryable();
    void Add(CrmQualityQuestion entity);
    void AddRange(IEnumerable<CrmQualityQuestion> entities);
    void Update(CrmQualityQuestion entity);
    void Delete(CrmQualityQuestion entity);
}
