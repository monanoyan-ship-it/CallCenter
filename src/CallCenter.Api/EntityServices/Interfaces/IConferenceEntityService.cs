using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IConferenceEntityService
{
    IQueryable<ConferenceRoom> GetRoomsQueryable();
    Task<ConferenceRoom?> GetRoomByIdAsync(int id);
    Task<ConferenceRoom?> GetRoomByIdWithParticipantsAsync(int id);
    Task<ConferenceParticipant?> GetParticipantByIdAsync(int id);
    void AddRoom(ConferenceRoom entity);
    void AddParticipant(ConferenceParticipant entity);
    void RemoveParticipant(ConferenceParticipant entity);
}
