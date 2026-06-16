using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class OrphanRecordingEntityService : IOrphanRecordingEntityService
{
    private readonly AppDbContext _db;

    public OrphanRecordingEntityService(AppDbContext db) => _db = db;

    public Task<OrphanRecording?> GetByUidAsync(Guid uid)
        => _db.OrphanRecordings.FirstOrDefaultAsync(o => o.Uid == uid);

    public Task<OrphanRecording?> GetByCloudFileIdAsync(int customerId, string cloudFileId)
        => _db.OrphanRecordings.FirstOrDefaultAsync(o =>
            o.CustomerId == customerId && o.CloudFileId == cloudFileId);

    public IQueryable<OrphanRecording> GetAllQueryable()
        => _db.OrphanRecordings.AsQueryable();

    public void Add(OrphanRecording entity) => _db.OrphanRecordings.Add(entity);
    public void Update(OrphanRecording entity) => _db.OrphanRecordings.Update(entity);
}
