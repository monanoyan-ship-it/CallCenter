using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IConferenceFactory
{
    Task<ConferenceRoomDto> CreateRoomAsync(CreateConferenceRequest req, int createdByUserId, int? customerId);
    Task<ConferenceRoomDto?> GetRoomAsync(int roomId);
    Task<List<ConferenceRoomDto>> GetActiveRoomsAsync(int? customerId);
    Task<(bool Success, string? Error)> AddParticipantAsync(int roomId, AddParticipantRequest req);
    Task<(bool Success, string? Error)> RemoveParticipantAsync(int roomId, int participantId);
    Task<(bool Success, string? Error)> MuteParticipantAsync(int roomId, int participantId, bool mute);
    Task<(bool Success, string? Error)> EndRoomAsync(int roomId);
}
