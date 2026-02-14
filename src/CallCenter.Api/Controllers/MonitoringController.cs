using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class MonitoringController : AuditableControllerBase
{
    public MonitoringController(ServiceFactory factory) : base(factory) { }

    /// <summary>Izleme oturumu baslat</summary>
    [HttpPost("start")]
    public async Task<ActionResult<MonitoringSessionDto>> StartMonitoring(StartMonitoringRequest req)
    {
        if (CurrentUserId == null) return Unauthorized();

        try
        {
            var svc = Factory.CreateMonitoringService();
            var session = await svc.StartMonitoringAsync(req, CurrentUserId.Value);

            await AuditCrudAsync("Create", "CallMonitoringSession", session.Id.ToString(),
                $"Izleme baslatildi: CallId={req.CallRecordId}, Mode={session.ModeName}");

            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Izleme modunu degistir (Silent/Whisper/Barge)</summary>
    [HttpPut("{id}/mode")]
    public async Task<ActionResult> ChangeMode(int id, [FromQuery] int modeId)
    {
        var svc = Factory.CreateMonitoringService();
        var (success, error) = await svc.ChangeModeAsync(id, modeId);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Update", "CallMonitoringSession", id.ToString(),
            $"Izleme modu degistirildi: ModeId={modeId}");

        return NoContent();
    }

    /// <summary>Izleme oturumunu durdur</summary>
    [HttpPost("{id}/stop")]
    public async Task<ActionResult> StopMonitoring(int id)
    {
        var svc = Factory.CreateMonitoringService();
        var (success, error) = await svc.StopMonitoringAsync(id);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Update", "CallMonitoringSession", id.ToString(), "Izleme durduruldu");

        return NoContent();
    }

    /// <summary>Aktif izleme oturumlari</summary>
    [HttpGet("active")]
    public async Task<ActionResult<List<MonitoringSessionDto>>> GetActiveSessions([FromQuery] int? customerId = null)
    {
        var svc = Factory.CreateMonitoringService();
        return Ok(await svc.GetActiveSessionsAsync(customerId));
    }

    /// <summary>Izlenebilir aktif aramalar</summary>
    [HttpGet("calls")]
    public async Task<ActionResult<List<MonitorableCallDto>>> GetMonitorableCalls([FromQuery] int? customerId = null)
    {
        var svc = Factory.CreateMonitoringService();
        return Ok(await svc.GetMonitorableCallsAsync(customerId));
    }
}
