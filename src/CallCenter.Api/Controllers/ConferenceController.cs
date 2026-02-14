using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class ConferenceController : AuditableControllerBase
{
    public ConferenceController(ServiceFactory factory) : base(factory) { }

    /// <summary>Yeni konferans odasi olustur</summary>
    [HttpPost("rooms")]
    public async Task<ActionResult<ConferenceRoomDto>> CreateRoom(CreateConferenceRequest req)
    {
        if (CurrentUserId == null) return Unauthorized();

        var svc = Factory.CreateConferenceService();
        var room = await svc.CreateRoomAsync(req, CurrentUserId.Value, CurrentCustomerId);

        await AuditCrudAsync("Create", "ConferenceRoom", room.Id.ToString(),
            $"Konferans odasi olusturuldu: '{req.Name}'", customerId: CurrentCustomerId);

        return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
    }

    /// <summary>Aktif konferans odalari</summary>
    [HttpGet("rooms/active")]
    public async Task<ActionResult<List<ConferenceRoomDto>>> GetActiveRooms([FromQuery] int? customerId = null)
    {
        var svc = Factory.CreateConferenceService();
        return Ok(await svc.GetActiveRoomsAsync(customerId));
    }

    /// <summary>Konferans odasi detay</summary>
    [HttpGet("rooms/{id}")]
    public async Task<ActionResult<ConferenceRoomDto>> GetRoom(int id)
    {
        var svc = Factory.CreateConferenceService();
        var room = await svc.GetRoomAsync(id);
        if (room == null) return NotFound(new { message = "Oda bulunamadi." });
        return Ok(room);
    }

    /// <summary>Konferansa katilimci ekle</summary>
    [HttpPost("rooms/{id}/participants")]
    public async Task<ActionResult> AddParticipant(int id, AddParticipantRequest req)
    {
        var svc = Factory.CreateConferenceService();
        var (success, error) = await svc.AddParticipantAsync(id, req);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "ConferenceParticipant", id.ToString(),
            $"Konferansa katilimci eklendi: UserId={req.UserId}, External={req.ExternalNumber}");

        return Ok();
    }

    /// <summary>Katilimciyi konferanstan cikar</summary>
    [HttpDelete("rooms/{id}/participants/{participantId}")]
    public async Task<ActionResult> RemoveParticipant(int id, int participantId)
    {
        var svc = Factory.CreateConferenceService();
        var (success, error) = await svc.RemoveParticipantAsync(id, participantId);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    /// <summary>Katilimciyi sessize al / ac</summary>
    [HttpPut("rooms/{id}/participants/{participantId}/mute")]
    public async Task<ActionResult> MuteParticipant(int id, int participantId, [FromQuery] bool mute = true)
    {
        var svc = Factory.CreateConferenceService();
        var (success, error) = await svc.MuteParticipantAsync(id, participantId, mute);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    /// <summary>Konferansi sonlandir</summary>
    [HttpPost("rooms/{id}/end")]
    public async Task<ActionResult> EndRoom(int id)
    {
        var svc = Factory.CreateConferenceService();
        var (success, error) = await svc.EndRoomAsync(id);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Update", "ConferenceRoom", id.ToString(), "Konferans odasi sonlandirildi");

        return NoContent();
    }
}
