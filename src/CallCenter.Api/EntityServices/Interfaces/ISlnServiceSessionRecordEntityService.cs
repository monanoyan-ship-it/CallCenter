using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnServiceSessionRecordEntityService
{
    IQueryable<SlnServiceSessionRecord> GetAllQueryable();
    Task<SlnServiceSessionRecord?> GetByIdAsync(int id);
    void Add(SlnServiceSessionRecord entity);
    void Update(SlnServiceSessionRecord entity);
    void Remove(SlnServiceSessionRecord entity);
}
