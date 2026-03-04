using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class ConferenceController : AuditableControllerBase
{
    private readonly IConferenceFactory _conferenceFactory;

    public ConferenceController(IAuditFactory auditFactory, IConferenceFactory conferenceFactory) : base(auditFactory)
    {
        _conferenceFactory = conferenceFactory;
    }

    [HttpPost("rooms")]
    public async Task<ActionResult<ConferenceRoomDto>> CreateRoom(CreateConferenceRequest req)
    {
        if (CurrentUserId == null) return Unauthorized();

        var room = await _conferenceFactory.CreateRoomAsync(req, CurrentUserId.Value, CurrentCustomerId);

        await AuditCrudAsync("Create", "ConferenceRoom", room.Id.ToString(),
            $"Konferans odasi olusturuldu: '{req.Name}'", customerId: CurrentCustomerId);

        return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
    }

    [HttpGet("rooms/active")]
    public async Task<ActionResult<List<ConferenceRoomDto>>> GetActiveRooms([FromQuery] int? customerId = null)
    {
        return Ok(await _conferenceFactory.GetActiveRoomsAsync(customerId));
    }

    [HttpGet("rooms/{id}")]
    public async Task<ActionResult<ConferenceRoomDto>> GetRoom(int id)
    {
        var room = await _conferenceFactory.GetRoomAsync(id);
        if (room == null) return NotFound(new { message = "Oda bulunamadi." });
        return Ok(room);
    }

    [HttpPost("rooms/{id}/participants")]
    public async Task<ActionResult> AddParticipant(int id, AddParticipantRequest req)
    {
        var (success, error) = await _conferenceFactory.AddParticipantAsync(id, req);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "ConferenceParticipant", id.ToString(),
            $"Konferansa katilimci eklendi: UserId={req.UserId}, External={req.ExternalNumber}");

        return Ok();
    }

    [HttpDelete("rooms/{id}/participants/{participantId}")]
    public async Task<ActionResult> RemoveParticipant(int id, int participantId)
    {
        var (success, error) = await _conferenceFactory.RemoveParticipantAsync(id, participantId);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpPut("rooms/{id}/participants/{participantId}/mute")]
    public async Task<ActionResult> MuteParticipant(int id, int participantId, [FromQuery] bool mute = true)
    {
        var (success, error) = await _conferenceFactory.MuteParticipantAsync(id, participantId, mute);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpPost("rooms/{id}/end")]
    public async Task<ActionResult> EndRoom(int id)
    {
        var (success, error) = await _conferenceFactory.EndRoomAsync(id);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Update", "ConferenceRoom", id.ToString(), "Konferans odasi sonlandirildi");

        return NoContent();
    }
}
