using CallCenter.Api.Services.Interfaces;
using CallCenter.Api.Services.MediaServer;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class MonitoringService : IMonitoringService
{
    private readonly AppDbContext _db;
    private readonly IJanusService _janus;
    private readonly ILogger<MonitoringService> _logger;

    public MonitoringService(AppDbContext db, IJanusService janus, ILogger<MonitoringService> logger)
    {
        _db = db;
        _janus = janus;
        _logger = logger;
    }

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

        // ─── Janus AudioBridge ile izleme baslat ───
        try
        {
            var janusAvailable = await _janus.IsAvailableAsync();
            if (janusAvailable)
            {
                var sessionId = await _janus.CreateSessionAsync();
                if (sessionId.HasValue)
                {
                    var handleId = await _janus.AttachPluginAsync(sessionId.Value, "janus.plugin.audiobridge");
                    if (handleId.HasValue)
                    {
                        // Aramanin AudioBridge room ID'sini bul (varsa)
                        // Yoksa yeni bir monitoring odasi olustur
                        long janusRoomId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        var roomCreated = await _janus.CreateAudioBridgeRoomAsync(
                            sessionId.Value, handleId.Value, janusRoomId,
                            $"Monitor-Call-{req.CallRecordId}", record: true);

                        if (roomCreated)
                        {
                            // Moda gore katilim tipi belirle
                            bool joined;
                            if (req.ModeId == MonitoringModes.Ids.Silent)
                            {
                                joined = await _janus.JoinAsListenerAsync(
                                    sessionId.Value, handleId.Value, janusRoomId, "Supervisor");
                            }
                            else if (req.ModeId == MonitoringModes.Ids.Whisper)
                            {
                                joined = await _janus.JoinAsListenerAsync(
                                    sessionId.Value, handleId.Value, janusRoomId, "Supervisor");
                                if (joined)
                                    await _janus.SwitchToWhisperAsync(sessionId.Value, handleId.Value);
                            }
                            else // Barge-In
                            {
                                joined = await _janus.JoinAudioBridgeRoomAsync(
                                    sessionId.Value, handleId.Value, janusRoomId, "Supervisor", muted: false);
                            }

                            if (joined)
                            {
                                session.MediaServerSessionId = $"{sessionId.Value}:{handleId.Value}:{janusRoomId}";
                                _logger.LogInformation("Janus monitoring baslatildi: Mode={Mode}, Room={RoomId}",
                                    MonitoringModes.GetById(req.ModeId)?.SystemName, janusRoomId);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Janus monitoring entegrasyonu basarisiz, DB-only mod devam ediyor");
        }

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

        // ─── Janus mod degistirme ───
        if (!string.IsNullOrEmpty(session.MediaServerSessionId))
        {
            try
            {
                var parts = session.MediaServerSessionId.Split(':');
                if (parts.Length == 3 &&
                    long.TryParse(parts[0], out var janusSessionId) &&
                    long.TryParse(parts[1], out var handleId))
                {
                    if (newModeId == MonitoringModes.Ids.Silent)
                    {
                        // Silent: supervisor mute
                        await _janus.ConfigureParticipantAsync(janusSessionId, handleId, muted: true);
                        _logger.LogInformation("Monitoring modu Silent'a gecirildi");
                    }
                    else if (newModeId == MonitoringModes.Ids.Whisper)
                    {
                        await _janus.SwitchToWhisperAsync(janusSessionId, handleId);
                        _logger.LogInformation("Monitoring modu Whisper'a gecirildi");
                    }
                    else // Barge-In
                    {
                        await _janus.SwitchToBargeInAsync(janusSessionId, handleId);
                        _logger.LogInformation("Monitoring modu Barge-In'e gecirildi");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Janus mod degistirme basarisiz");
            }
        }

        session.ModeId = newModeId;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> StopMonitoringAsync(int sessionId)
    {
        var session = await _db.CallMonitoringSessions.FindAsync(sessionId);
        if (session == null) return (false, "Izleme oturumu bulunamadi");
        if (session.EndedAt != null) return (false, "Izleme oturumu zaten sonlanmis");

        // ─── Janus session temizleme ───
        if (!string.IsNullOrEmpty(session.MediaServerSessionId))
        {
            try
            {
                var parts = session.MediaServerSessionId.Split(':');
                if (parts.Length == 3 &&
                    long.TryParse(parts[0], out var janusSessionId) &&
                    long.TryParse(parts[1], out var handleId) &&
                    long.TryParse(parts[2], out var janusRoomId))
                {
                    await _janus.LeaveAudioBridgeRoomAsync(janusSessionId, handleId);
                    await _janus.DestroyAudioBridgeRoomAsync(janusSessionId, handleId, janusRoomId);
                    await _janus.DestroySessionAsync(janusSessionId);
                    _logger.LogInformation("Janus monitoring oturumu temizlendi: {SessionId}", janusSessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Janus monitoring temizleme basarisiz");
            }
        }

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
