using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class ConferenceService : IConferenceService
{
    private readonly AppDbContext _db;

    public ConferenceService(AppDbContext db) => _db = db;

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
            .FirstOrDefaultAsync(p => p.Id == participantId && p.ConferenceRoomId == roomId);

        if (participant == null) return (false, "Katilimci bulunamadi");

        participant.StatusId = ConferenceParticipantStatuses.Ids.Kicked;
        participant.LeftAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> MuteParticipantAsync(int roomId, int participantId, bool mute)
    {
        var participant = await _db.ConferenceParticipants
            .FirstOrDefaultAsync(p => p.Id == participantId && p.ConferenceRoomId == roomId);

        if (participant == null) return (false, "Katilimci bulunamadi");

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
