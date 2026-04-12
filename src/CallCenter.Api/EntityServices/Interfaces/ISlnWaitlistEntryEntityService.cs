using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnWaitlistEntryEntityService
{
    IQueryable<SlnWaitlistEntry> GetAllQueryable();
    Task<SlnWaitlistEntry?> GetByIdAsync(int id);
    void Add(SlnWaitlistEntry entity);
    void Update(SlnWaitlistEntry entity);
    void Remove(SlnWaitlistEntry entity);
}
