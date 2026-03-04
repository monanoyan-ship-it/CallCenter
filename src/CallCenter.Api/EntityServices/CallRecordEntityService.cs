using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class CallRecordEntityService : ICallRecordEntityService
{
    private readonly AppDbContext _db;

    public CallRecordEntityService(AppDbContext db) => _db = db;

    public Task<CallRecord?> GetByIdAsync(int id)
        => _db.CallRecords.FindAsync(id).AsTask();

    public Task<CallRecord?> GetByUidAsync(Guid uid)
        => _db.CallRecords.FirstOrDefaultAsync(c => c.Uid == uid);

    public IQueryable<CallRecord> GetAllQueryable()
        => _db.CallRecords.AsQueryable();

    public void Add(CallRecord entity) => _db.CallRecords.Add(entity);
    public void Update(CallRecord entity) => _db.CallRecords.Update(entity);
}
