using CallCenter.Api.Services.Interfaces;
using CallCenter.Api.Services.MediaServer;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class ConferenceService : IConferenceService
{
    private readonly AppDbContext _db;
    private readonly IJanusService _janus;
    private readonly ILogger<ConferenceService> _logger;

    public ConferenceService(AppDbContext db, IJanusService janus, ILogger<ConferenceService> logger)
    {
        _db = db;
        _janus = janus;
        _logger = logger;
    }

    public async Task<ConferenceRoomDto> CreateRoomAsync(CreateConferenceRequest req, int createdByUserId, int? customerId)
    {
        var room = new ConferenceRoom
        {
            Name = req.Name,
            StatusId = ConferenceStatuses.Ids.Active,
            CreatedByUserId = createdByUserId,
            CustomerId = customerId ?? req.CustomerId,
            MaxParticipants = req.MaxParticipants
        };

        // ─── Janus AudioBridge oda olusturma ───
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
                        // Janus room ID olarak DB oda ID'si yerine benzersiz long kullan
                        long janusRoomId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        var created = await _janus.CreateAudioBridgeRoomAsync(
                            sessionId.Value, handleId.Value, janusRoomId,
                            req.Name, record: true);

                        if (created)
                        {
                            room.MediaServerRoomId = $"{sessionId.Value}:{handleId.Value}:{janusRoomId}";
                            _logger.LogInformation("Janus AudioBridge odasi olusturuldu: {RoomId}", janusRoomId);
                        }
                        else
                        {
                            _logger.LogWarning("Janus AudioBridge oda olusturulamadi, DB-only mod devam ediyor");
                        }
                    }
                }
            }
            else
            {
                _logger.LogInformation("Janus Gateway erisilemez, konferans DB-only modda olusturuluyor");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Janus AudioBridge entegrasyonu basarisiz, DB-only mod devam ediyor");
        }

        // Olusturan kisiyi Host olarak ekle
        room.Participants.Add(new ConferenceParticipant
        {
            UserId = createdByUserId,
            RoleId = ConferenceParticipantRoles.Ids.Host,
            StatusId = ConferenceParticipantStatuses.Ids.Joined
        });

        _db.ConferenceRooms.Add(room);
        await _db.SaveChangesAsync();

        return await GetRoomAsync(room.Id) ?? throw new InvalidOperationException("Oda olusturulamadi");
    }

    public async Task<ConferenceRoomDto?> GetRoomAsync(int roomId)
    {
        var room = await _db.ConferenceRooms
            .Include(r => r.CreatedByUser)
            .Include(r => r.Participants).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        return room == null ? null : MapToDto(room);
    }

    public async Task<List<ConferenceRoomDto>> GetActiveRoomsAsync(int? customerId)
    {
        var query = _db.ConferenceRooms
            .Include(r => r.CreatedByUser)
            .Include(r => r.Participants).ThenInclude(p => p.User)
            .Where(r => r.StatusId == ConferenceStatuses.Ids.Active);

        if (customerId.HasValue)
            query = query.Where(r => r.CustomerId == customerId.Value);

        var rooms = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return rooms.Select(MapToDto).ToList();
    }

    public async Task<(bool Success, string? Error)> AddParticipantAsync(int roomId, AddParticipantRequest req)
    {
        var room = await _db.ConferenceRooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null) return (false, "Oda bulunamadi");
        if (room.StatusId != ConferenceStatuses.Ids.Active) return (false, "Oda aktif degil");
        if (room.Participants.Count(p => p.StatusId != ConferenceParticipantStatuses.Ids.Left &&
                                         p.StatusId != ConferenceParticipantStatuses.Ids.Kicked)
            >= room.MaxParticipants)
            return (false, "Maksimum katilimci sayisina ulasildi");

        if (req.UserId == null && string.IsNullOrEmpty(req.ExternalNumber))
            return (false, "UserId veya ExternalNumber gerekli");

        var participant = new ConferenceParticipant
        {
            ConferenceRoomId = roomId,
            UserId = req.UserId,
            ExternalNumber = req.ExternalNumber,
            RoleId = req.RoleId,
            StatusId = ConferenceParticipantStatuses.Ids.Invited
        };

        _db.ConferenceParticipants.Add(participant);
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveParticipantAsync(int roomId, int participantId)
    {
        var participant = await _db.ConferenceParticipants
            .Include(p => p.ConferenceRoom)
            .FirstOrDefaultAsync(p => p.Id == participantId && p.ConferenceRoomId == roomId);

        if (participant == null) return (false, "Katilimci bulunamadi");

        // ─── Janus AudioBridge katilimciyi cikar ───
        var mediaRoomId = participant.ConferenceRoom?.MediaServerRoomId;
        if (!string.IsNullOrEmpty(mediaRoomId))
        {
            try
            {
                var parts = mediaRoomId.Split(':');
                if (parts.Length == 3 &&
                    long.TryParse(parts[0], out var sessionId) &&
                    long.TryParse(parts[1], out var handleId))
                {
                    await _janus.LeaveAudioBridgeRoomAsync(sessionId, handleId);
                    _logger.LogInformation("Janus katilimci cikarildi: {ParticipantId}", participantId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Janus leave basarisiz: {ParticipantId}", participantId);
            }
        }

        participant.StatusId = ConferenceParticipantStatuses.Ids.Kicked;
        participant.LeftAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> MuteParticipantAsync(int roomId, int participantId, bool mute)
    {
        var participant = await _db.ConferenceParticipants
            .Include(p => p.ConferenceRoom)
            .FirstOrDefaultAsync(p => p.Id == participantId && p.ConferenceRoomId == roomId);

        if (participant == null) return (false, "Katilimci bulunamadi");

        // ─── Janus AudioBridge mute/unmute ───
        var mediaRoomId = participant.ConferenceRoom?.MediaServerRoomId;
        if (!string.IsNullOrEmpty(mediaRoomId))
        {
            try
            {
                var parts = mediaRoomId.Split(':');
                if (parts.Length == 3 &&
                    long.TryParse(parts[0], out var sessionId) &&
                    long.TryParse(parts[1], out var handleId))
                {
                    await _janus.ConfigureParticipantAsync(sessionId, handleId, muted: mute);
                    _logger.LogInformation("Janus katilimci {Action}: {ParticipantId}", mute ? "muted" : "unmuted", participantId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Janus mute/unmute basarisiz: {ParticipantId}", participantId);
            }
        }

        participant.IsMuted = mute;
        if (mute)
            participant.StatusId = ConferenceParticipantStatuses.Ids.Muted;
        else if (participant.StatusId == ConferenceParticipantStatuses.Ids.Muted)
            participant.StatusId = ConferenceParticipantStatuses.Ids.Joined;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> EndRoomAsync(int roomId)
    {
        var room = await _db.ConferenceRooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null) return (false, "Oda bulunamadi");
        if (room.StatusId != ConferenceStatuses.Ids.Active) return (false, "Oda zaten sonlanmis");

        // ─── Janus AudioBridge oda silme ───
        if (!string.IsNullOrEmpty(room.MediaServerRoomId))
        {
            try
            {
                var parts = room.MediaServerRoomId.Split(':');
                if (parts.Length == 3 &&
                    long.TryParse(parts[0], out var sessionId) &&
                    long.TryParse(parts[1], out var handleId) &&
                    long.TryParse(parts[2], out var janusRoomId))
                {
                    await _janus.DestroyAudioBridgeRoomAsync(sessionId, handleId, janusRoomId);
                    await _janus.DestroySessionAsync(sessionId);
                    _logger.LogInformation("Janus AudioBridge odasi silindi: {RoomId}", janusRoomId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Janus AudioBridge oda silme basarisiz: {RoomId}", room.MediaServerRoomId);
            }
        }

        room.StatusId = ConferenceStatuses.Ids.Ended;
        room.EndedAt = DateTime.UtcNow;

        foreach (var p in room.Participants.Where(p => p.LeftAt == null))
        {
            p.StatusId = ConferenceParticipantStatuses.Ids.Left;
            p.LeftAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    private static ConferenceRoomDto MapToDto(ConferenceRoom r) => new()
    {
        Id = r.Id,
        Uid = r.Uid,
        Name = r.Name,
        StatusId = r.StatusId,
        StatusName = ConferenceStatuses.GetById(r.StatusId)?.SystemName ?? "Unknown",
        CreatedByUserId = r.CreatedByUserId,
        CreatedByUserName = r.CreatedByUser?.FullName,
        CustomerId = r.CustomerId,
        MaxParticipants = r.MaxParticipants,
        ParticipantCount = r.Participants.Count(p => p.StatusId == ConferenceParticipantStatuses.Ids.Joined),
        CreatedAt = r.CreatedAt,
        EndedAt = r.EndedAt,
        Participants = r.Participants.Select(p => new ConferenceParticipantDto
        {
            Id = p.Id,
            UserId = p.UserId,
            UserName = p.User?.FullName,
            ExternalNumber = p.ExternalNumber,
            RoleId = p.RoleId,
            RoleName = ConferenceParticipantRoles.GetById(p.RoleId)?.SystemName ?? "Unknown",
            StatusId = p.StatusId,
            StatusName = ConferenceParticipantStatuses.GetById(p.StatusId)?.SystemName ?? "Unknown",
            IsMuted = p.IsMuted,
            JoinedAt = p.JoinedAt,
            LeftAt = p.LeftAt
        }).ToList()
    };
}
