using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnAutoReminderEntityService
{
    IQueryable<SlnAutoReminder> GetAllQueryable();
    Task<SlnAutoReminder?> GetByIdAsync(int id);
    void Add(SlnAutoReminder entity);
    void Update(SlnAutoReminder entity);
    void Remove(SlnAutoReminder entity);
}
