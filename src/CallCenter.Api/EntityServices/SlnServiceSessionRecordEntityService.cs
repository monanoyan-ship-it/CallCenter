using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnServiceSessionRecordEntityService : ISlnServiceSessionRecordEntityService
{
    private readonly AppDbContext _db;

    public SlnServiceSessionRecordEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnServiceSessionRecord> GetAllQueryable()
        => _db.SlnServiceSessionRecords.AsQueryable();

    public Task<SlnServiceSessionRecord?> GetByIdAsync(int id)
        => _db.SlnServiceSessionRecords.FindAsync(id).AsTask();

    public void Add(SlnServiceSessionRecord entity) => _db.SlnServiceSessionRecords.Add(entity);
    public void Update(SlnServiceSessionRecord entity) => _db.SlnServiceSessionRecords.Update(entity);
    public void Remove(SlnServiceSessionRecord entity) => _db.SlnServiceSessionRecords.Remove(entity);
}
