using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class ConferenceEntityService : IConferenceEntityService
{
    private readonly AppDbContext _db;

    public ConferenceEntityService(AppDbContext db) => _db = db;

    public IQueryable<ConferenceRoom> GetRoomsQueryable()
        => _db.ConferenceRooms.AsQueryable();

    public Task<ConferenceRoom?> GetRoomByIdAsync(int id)
        => _db.ConferenceRooms.FindAsync(id).AsTask();

    public Task<ConferenceRoom?> GetRoomByIdWithParticipantsAsync(int id)
        => _db.ConferenceRooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == id);

    public Task<ConferenceParticipant?> GetParticipantByIdAsync(int id)
        => _db.ConferenceParticipants.FindAsync(id).AsTask();

    public void AddRoom(ConferenceRoom entity) => _db.ConferenceRooms.Add(entity);
    public void AddParticipant(ConferenceParticipant entity) => _db.ConferenceParticipants.Add(entity);
    public void RemoveParticipant(ConferenceParticipant entity) => _db.ConferenceParticipants.Remove(entity);
}
