using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnAutoReminderEntityService : ISlnAutoReminderEntityService
{
    private readonly AppDbContext _db;

    public SlnAutoReminderEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnAutoReminder> GetAllQueryable()
        => _db.SlnAutoReminders.AsQueryable();

    public Task<SlnAutoReminder?> GetByIdAsync(int id)
        => _db.SlnAutoReminders.FindAsync(id).AsTask();

    public void Add(SlnAutoReminder entity) => _db.SlnAutoReminders.Add(entity);
    public void Update(SlnAutoReminder entity) => _db.SlnAutoReminders.Update(entity);
    public void Remove(SlnAutoReminder entity) => _db.SlnAutoReminders.Remove(entity);
}
