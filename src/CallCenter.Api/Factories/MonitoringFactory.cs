using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services.MediaServer;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CallCenter.Api.Factories;

public class MonitoringFactory : IMonitoringFactory
{
    private readonly IMonitoringSessionEntityService _monitoringEs;
    private readonly ICallRecordEntityService _callEs;
    private readonly IJanusService _janus;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<MonitoringFactory> _logger;

    public MonitoringFactory(
        IMonitoringSessionEntityService monitoringEs,
        ICallRecordEntityService callEs,
        IJanusService janus,
        IUnitOfWork uow,
        ILogger<MonitoringFactory> logger)
    {
        _monitoringEs = monitoringEs;
        _callEs = callEs;
        _janus = janus;
        _uow = uow;
        _logger = logger;
    }

    public async Task<MonitoringSessionDto> StartMonitoringAsync(StartMonitoringRequest req, int supervisorId)
    {
        var call = await _callEs.GetAllQueryable()
            .Include(c => c.Agent)
            .FirstOrDefaultAsync(c => c.Id == req.CallRecordId);

        if (call == null)
            throw new InvalidOperationException("Arama bulunamadi");

        var activeStatuses = new[] { CallStatuses.Ids.Ringing, CallStatuses.Ids.InProgress, CallStatuses.Ids.OnHold };
        if (!activeStatuses.Contains(call.StatusId))
            throw new InvalidOperationException("Arama aktif degil, izleme baslatilamaz");

        var session = new CallMonitoringSession
        {
            CallRecordId = req.CallRecordId,
            SupervisorId = supervisorId,
            ModeId = req.ModeId
        };

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
                        long janusRoomId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        var roomCreated = await _janus.CreateAudioBridgeRoomAsync(
                            sessionId.Value, handleId.Value, janusRoomId,
                            $"Monitor-Call-{req.CallRecordId}", record: true);

                        if (roomCreated)
                        {
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
                            else
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

        _monitoringEs.Add(session);
        await _uow.SaveChangesAsync();

        return MapToDto(session, call);
    }

    public async Task<(bool Success, string? Error)> ChangeModeAsync(int sessionId, int newModeId)
    {
        var session = await _monitoringEs.GetByIdAsync(sessionId);
        if (session == null) return (false, "Izleme oturumu bulunamadi");
        if (session.EndedAt != null) return (false, "Izleme oturumu sonlanmis");

        if (MonitoringModes.GetById(newModeId) == null)
            return (false, "Gecersiz izleme modu");

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
                        await _janus.ConfigureParticipantAsync(janusSessionId, handleId, muted: true);
                        _logger.LogInformation("Monitoring modu Silent'a gecirildi");
                    }
                    else if (newModeId == MonitoringModes.Ids.Whisper)
                    {
                        await _janus.SwitchToWhisperAsync(janusSessionId, handleId);
                        _logger.LogInformation("Monitoring modu Whisper'a gecirildi");
                    }
                    else
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
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> StopMonitoringAsync(int sessionId)
    {
        var session = await _monitoringEs.GetByIdAsync(sessionId);
        if (session == null) return (false, "Izleme oturumu bulunamadi");
        if (session.EndedAt != null) return (false, "Izleme oturumu zaten sonlanmis");

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
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<List<MonitoringSessionDto>> GetActiveSessionsAsync(int? customerId)
    {
        var query = _monitoringEs.GetAllQueryable()
            .Include(m => m.CallRecord).ThenInclude(c => c!.Agent)
            .Include(m => m.Supervisor)
            .Where(m => m.EndedAt == null);

        var sessions = await query.OrderByDescending(m => m.StartedAt).ToListAsync();
        return sessions.Select(s => MapToDto(s, s.CallRecord!)).ToList();
    }

    public async Task<List<MonitorableCallDto>> GetMonitorableCallsAsync(int? customerId)
    {
        var activeStatuses = new[] { CallStatuses.Ids.InProgress, CallStatuses.Ids.OnHold };

        var query = _callEs.GetAllQueryable()
            .Include(c => c.Agent)
            .Include(c => c.Queue)
            .Where(c => activeStatuses.Contains(c.StatusId));

        var calls = await query.OrderByDescending(c => c.StartedAt).ToListAsync();

        var monitoredCallIds = await _monitoringEs.GetAllQueryable()
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
