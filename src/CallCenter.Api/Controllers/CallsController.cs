using System.Security.Claims;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CallsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<CallCenterHub> _hub;

    public CallsController(AppDbContext db, IHubContext<CallCenterHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    /// <summary>Arama gecmisi (tamamlanmis aramalar)</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();

        var query = _db.CallRecords
            .Where(c => c.AgentId == userId)
            .Where(c => CallStatuses.FinishedStatuses.Select(s => s.Id).Contains(c.StatusId))
            .OrderByDescending(c => c.StartedAt);

        var records = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.CallerNumber,
                c.CalleeNumber,
                c.DirectionId,
                c.StatusId,
                c.StartedAt,
                c.DurationSeconds,
                QueueName = c.Queue != null ? c.Queue.Name : null
            })
            .ToListAsync();

        return Ok(records);
    }

    /// <summary>Aktif aramalar (devam eden)</summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var userId = GetUserId();

        var records = await _db.CallRecords
            .Where(c => c.AgentId == userId)
            .Where(c => CallStatuses.ActiveStatuses.Select(s => s.Id).Contains(c.StatusId))
            .OrderByDescending(c => c.StartedAt)
            .Select(c => new
            {
                c.Id,
                c.CallerNumber,
                c.CalleeNumber,
                c.DirectionId,
                c.StatusId,
                c.StartedAt,
                QueueName = c.Queue != null ? c.Queue.Name : null
            })
            .ToListAsync();

        return Ok(records);
    }

    /// <summary>Yeni arama baslat (outbound)</summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartCall([FromBody] StartCallRequest request)
    {
        var userId = GetUserId();

        var call = new CallRecord
        {
            CallerNumber = request.CallerNumber,
            CalleeNumber = request.CalleeNumber,
            DirectionId = CallDirections.Ids.Outbound,
            StatusId = CallStatuses.Ids.Ringing,
            StartedAt = DateTime.UtcNow,
            AgentId = userId,
            QueueId = request.QueueId
        };

        _db.CallRecords.Add(call);
        await _db.SaveChangesAsync();

        // Agent durumunu InCall yap
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.StatusId = AgentStatuses.Ids.InCall;
            await _db.SaveChangesAsync();
        }

        return Ok(new { call.Id, call.Uid });
    }

    /// <summary>Aramayi beklet</summary>
    [HttpPut("{callId}/hold")]
    public async Task<IActionResult> HoldCall(int callId)
    {
        var call = await _db.CallRecords.FindAsync(callId);
        if (call == null) return NotFound();

        call.StatusId = CallStatuses.Ids.OnHold;
        await _db.SaveChangesAsync();

        return Ok();
    }

    /// <summary>Aramayi sonlandir</summary>
    [HttpPut("{callId}/end")]
    public async Task<IActionResult> EndCall(int callId)
    {
        var call = await _db.CallRecords.FindAsync(callId);
        if (call == null) return NotFound();

        call.StatusId = CallStatuses.Ids.Completed;
        call.EndedAt = DateTime.UtcNow;
        if (call.AnsweredAt.HasValue)
        {
            call.DurationSeconds = (int)(call.EndedAt.Value - call.AnsweredAt.Value).TotalSeconds;
        }
        await _db.SaveChangesAsync();

        // Hub'a bildir
        await _hub.Clients.All.SendAsync("CallEnded", callId);

        // Agent durumunu AfterCallWork yap
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.StatusId = AgentStatuses.Ids.AfterCallWork;
            await _db.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("AgentStatusChanged", new AgentStatusUpdate
            {
                AgentId = user.Id,
                AgentName = user.FullName,
                StatusId = AgentStatuses.Ids.AfterCallWork,
                StatusName = AgentStatuses.AfterCallWork.SystemName
            });
        }

        return Ok();
    }

    /// <summary>Aramayi cevapla</summary>
    [HttpPut("{callId}/answer")]
    public async Task<IActionResult> AnswerCall(int callId)
    {
        var call = await _db.CallRecords.FindAsync(callId);
        if (call == null) return NotFound();

        call.StatusId = CallStatuses.Ids.InProgress;
        call.AnsweredAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok();
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}

/// <summary>Yeni arama baslatma request'i</summary>
public class StartCallRequest
{
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public int? QueueId { get; set; }
}
