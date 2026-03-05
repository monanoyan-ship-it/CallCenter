using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IConsentRecordEntityService
{
    IQueryable<ConsentRecord> GetAllQueryable();
    Task<ConsentRecord?> GetByIdAsync(int id);
    Task<ConsentRecord?> GetByUidAsync(Guid uid);
    void Add(ConsentRecord entity);
    void Update(ConsentRecord entity);
    void Remove(ConsentRecord entity);
}
