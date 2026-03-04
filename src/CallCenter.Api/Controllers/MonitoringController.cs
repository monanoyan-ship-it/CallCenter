using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class MonitoringController : AuditableControllerBase
{
    private readonly IMonitoringFactory _monitoringFactory;

    public MonitoringController(IAuditFactory auditFactory, IMonitoringFactory monitoringFactory) : base(auditFactory)
    {
        _monitoringFactory = monitoringFactory;
    }

    [HttpPost("start")]
    public async Task<ActionResult<MonitoringSessionDto>> StartMonitoring(StartMonitoringRequest req)
    {
        if (CurrentUserId == null) return Unauthorized();

        try
        {
            var session = await _monitoringFactory.StartMonitoringAsync(req, CurrentUserId.Value);

            await AuditCrudAsync("Create", "CallMonitoringSession", session.Id.ToString(),
                $"Izleme baslatildi: CallId={req.CallRecordId}, Mode={session.ModeName}");

            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/mode")]
    public async Task<ActionResult> ChangeMode(int id, [FromQuery] int modeId)
    {
        var (success, error) = await _monitoringFactory.ChangeModeAsync(id, modeId);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Update", "CallMonitoringSession", id.ToString(),
            $"Izleme modu degistirildi: ModeId={modeId}");

        return NoContent();
    }

    [HttpPost("{id}/stop")]
    public async Task<ActionResult> StopMonitoring(int id)
    {
        var (success, error) = await _monitoringFactory.StopMonitoringAsync(id);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Update", "CallMonitoringSession", id.ToString(), "Izleme durduruldu");

        return NoContent();
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<MonitoringSessionDto>>> GetActiveSessions([FromQuery] int? customerId = null)
    {
        return Ok(await _monitoringFactory.GetActiveSessionsAsync(customerId));
    }

    [HttpGet("calls")]
    public async Task<ActionResult<List<MonitorableCallDto>>> GetMonitorableCalls([FromQuery] int? customerId = null)
    {
        return Ok(await _monitoringFactory.GetMonitorableCallsAsync(customerId));
    }
}
