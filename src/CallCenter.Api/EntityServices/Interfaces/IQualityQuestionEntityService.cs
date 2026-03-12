using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IQualityQuestionEntityService
{
    Task<QualityQuestion?> GetByIdAsync(int id);
    IQueryable<QualityQuestion> GetAllQueryable();
    void Add(QualityQuestion entity);
    void AddRange(IEnumerable<QualityQuestion> entities);
    void Update(QualityQuestion entity);
    void Delete(QualityQuestion entity);
}
