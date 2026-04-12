using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnWaitlistEntryEntityService : ISlnWaitlistEntryEntityService
{
    private readonly AppDbContext _db;

    public SlnWaitlistEntryEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnWaitlistEntry> GetAllQueryable()
        => _db.SlnWaitlistEntries.AsQueryable();

    public Task<SlnWaitlistEntry?> GetByIdAsync(int id)
        => _db.SlnWaitlistEntries.FindAsync(id).AsTask();

    public void Add(SlnWaitlistEntry entity) => _db.SlnWaitlistEntries.Add(entity);
    public void Update(SlnWaitlistEntry entity) => _db.SlnWaitlistEntries.Update(entity);
    public void Remove(SlnWaitlistEntry entity) => _db.SlnWaitlistEntries.Remove(entity);
}
