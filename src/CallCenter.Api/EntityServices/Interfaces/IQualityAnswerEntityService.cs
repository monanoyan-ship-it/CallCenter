using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IQualityAnswerEntityService
{
    Task<QualityAnswer?> GetByIdAsync(int id);
    IQueryable<QualityAnswer> GetAllQueryable();
    void Add(QualityAnswer entity);
    void AddRange(IEnumerable<QualityAnswer> entities);
    void Update(QualityAnswer entity);
    void DeleteRange(IEnumerable<QualityAnswer> entities);
}
