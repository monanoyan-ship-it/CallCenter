using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class RecordingAccessLogEntityService : IRecordingAccessLogEntityService
{
    private readonly AppDbContext _db;

    public RecordingAccessLogEntityService(AppDbContext db) => _db = db;

    public void Add(RecordingAccessLog entity) => _db.RecordingAccessLogs.Add(entity);

    public Task<List<RecordingAccessLog>> GetByCallRecordIdAsync(int callRecordId)
        => _db.RecordingAccessLogs
            .Where(r => r.CallRecordId == callRecordId)
            .OrderByDescending(r => r.AccessedAt)
            .ToListAsync();
}
