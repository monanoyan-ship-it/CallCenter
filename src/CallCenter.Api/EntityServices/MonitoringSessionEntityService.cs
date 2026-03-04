using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class MonitoringSessionEntityService : IMonitoringSessionEntityService
{
    private readonly AppDbContext _db;

    public MonitoringSessionEntityService(AppDbContext db) => _db = db;

    public IQueryable<CallMonitoringSession> GetAllQueryable()
        => _db.CallMonitoringSessions.AsQueryable();

    public Task<CallMonitoringSession?> GetByIdAsync(int id)
        => _db.CallMonitoringSessions.FindAsync(id).AsTask();

    public void Add(CallMonitoringSession entity) => _db.CallMonitoringSessions.Add(entity);
}
