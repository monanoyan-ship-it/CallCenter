using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class ConsentRecordEntityService : IConsentRecordEntityService
{
    private readonly AppDbContext _db;

    public ConsentRecordEntityService(AppDbContext db) => _db = db;

    public IQueryable<ConsentRecord> GetAllQueryable() => _db.ConsentRecords.AsQueryable();
    public Task<ConsentRecord?> GetByIdAsync(int id) => _db.ConsentRecords.FindAsync(id).AsTask();
    public Task<ConsentRecord?> GetByUidAsync(Guid uid) => _db.ConsentRecords.FirstOrDefaultAsync(c => c.Uid == uid);
    public void Add(ConsentRecord entity) => _db.ConsentRecords.Add(entity);
    public void Update(ConsentRecord entity) => _db.ConsentRecords.Update(entity);
    public void Remove(ConsentRecord entity) => _db.ConsentRecords.Remove(entity);
}
