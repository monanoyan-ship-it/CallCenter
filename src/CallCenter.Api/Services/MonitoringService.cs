using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class MonitoringService : IMonitoringService
{
    private readonly AppDbContext _db;

    public MonitoringService(AppDbContext db) => _db = db;

    public async Task<MonitoringSessionDto> StartMonitoringAsync(StartMonitoringRequest req, int supervisorId)
    {
        var call = await _db.CallRecords
            .Include(c => c.Agent)
            .FirstOrDefaultAsync(c => c.Id == req.CallRecordId);

        if (call == null)
            throw new InvalidOperationException("Arama bulunamadi");

        // Aktif arama mi kontrol et
        var activeStatuses = new[] { CallStatuses.Ids.Ringing, CallStatuses.Ids.InProgress, CallStatuses.Ids.OnHold };
        if (!activeStatuses.Contains(call.StatusId))
            throw new InvalidOperationException("Arama aktif degil, izleme baslatilamaz");

        var session = new CallMonitoringSession
        {
            CallRecordId = req.CallRecordId,
            SupervisorId = supervisorId,
            ModeId = req.ModeId
        };

        _db.CallMonitoringSessions.Add(session);
        await _db.SaveChangesAsync();

        return MapToDto(session, call);
    }

    public async Task<(bool Success, string? Error)> ChangeModeAsync(int sessionId, int newModeId)
    {
        var session = await _db.CallMonitoringSessions.FindAsync(sessionId);
        if (session == null) return (false, "Izleme oturumu bulunamadi");
        if (session.EndedAt != null) return (false, "Izleme oturumu sonlanmis");

        if (MonitoringModes.GetById(newModeId) == null)
            return (false, "Gecersiz izleme modu");

        session.ModeId = newModeId;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> StopMonitoringAsync(int sessionId)
    {
        var session = await _db.CallMonitoringSessions.FindAsync(sessionId);
        if (session == null) return (false, "Izleme oturumu bulunamadi");
        if (session.EndedAt != null) return (false, "Izleme oturumu zaten sonlanmis");

        session.EndedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<List<MonitoringSessionDto>> GetActiveSessionsAsync(int? customerId)
    {
        var query = _db.CallMonitoringSessions
            .Include(m => m.CallRecord).ThenInclude(c => c!.Agent)
            .Include(m => m.Supervisor)
            .Where(m => m.EndedAt == null);

        if (customerId.HasValue)
        {
            // Firma bazli filtreleme: agent'in firma iliskisi uzerinden
            // (basitlestirme: tum aktif izleme oturumlarini dondur)
        }

        var sessions = await query.OrderByDescending(m => m.StartedAt).ToListAsync();
        return sessions.Select(s => MapToDto(s, s.CallRecord!)).ToList();
    }

    public async Task<List<MonitorableCallDto>> GetMonitorableCallsAsync(int? customerId)
    {
        var activeStatuses = new[] { CallStatuses.Ids.InProgress, CallStatuses.Ids.OnHold };

        var query = _db.CallRecords
            .Include(c => c.Agent)
            .Include(c => c.Queue)
            .Where(c => activeStatuses.Contains(c.StatusId));

        var calls = await query.OrderByDescending(c => c.StartedAt).ToListAsync();

        // Aktif izleme oturumlari
        var monitoredCallIds = await _db.CallMonitoringSessions
            .Where(m => m.EndedAt == null)
            .Select(m => m.CallRecordId)
            .ToListAsync();

        return calls.Select(c => new MonitorableCallDto
        {
            CallRecordId = c.Id,
            CallerNumber = c.CallerNumber,
            CalleeNumber = c.CalleeNumber,
            AgentId = c.AgentId,
            AgentName = c.Agent?.FullName,
            AgentExtension = c.Agent?.Extension,
            StatusId = c.StatusId,
            StatusName = CallStatuses.GetById(c.StatusId)?.SystemName ?? "Unknown",
            QueueName = c.Queue?.Name,
            StartedAt = c.StartedAt,
            DurationSeconds = c.AnsweredAt.HasValue
                ? (int)(DateTime.UtcNow - c.AnsweredAt.Value).TotalSeconds
                : 0,
            IsBeingMonitored = monitoredCallIds.Contains(c.Id)
        }).ToList();
    }

    private static MonitoringSessionDto MapToDto(CallMonitoringSession s, CallRecord c) => new()
    {
        Id = s.Id,
        Uid = s.Uid,
        CallRecordId = s.CallRecordId,
        CallerNumber = c.CallerNumber,
        CalleeNumber = c.CalleeNumber,
        AgentId = c.AgentId,
        AgentName = c.Agent?.FullName,
        SupervisorId = s.SupervisorId,
        SupervisorName = s.Supervisor?.FullName,
        ModeId = s.ModeId,
        ModeName = MonitoringModes.GetById(s.ModeId)?.SystemName ?? "Unknown",
        StartedAt = s.StartedAt,
        EndedAt = s.EndedAt,
        Notes = s.Notes
    };
}
