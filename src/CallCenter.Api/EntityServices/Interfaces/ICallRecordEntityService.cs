using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICallRecordEntityService
{
    Task<CallRecord?> GetByIdAsync(int id);
    Task<CallRecord?> GetByUidAsync(Guid uid);
    IQueryable<CallRecord> GetAllQueryable();
    void Add(CallRecord entity);
    void Update(CallRecord entity);
}
