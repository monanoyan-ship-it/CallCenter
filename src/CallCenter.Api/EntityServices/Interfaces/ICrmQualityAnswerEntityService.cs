using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICrmQualityAnswerEntityService
{
    Task<CrmQualityAnswer?> GetByIdAsync(int id);
    IQueryable<CrmQualityAnswer> GetAllQueryable();
    void Add(CrmQualityAnswer entity);
    void AddRange(IEnumerable<CrmQualityAnswer> entities);
    void Update(CrmQualityAnswer entity);
    void DeleteRange(IEnumerable<CrmQualityAnswer> entities);
}
