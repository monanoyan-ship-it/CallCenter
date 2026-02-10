using CallCenter.Api.Hubs;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class CallService : ICallService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<CallCenterHub> _hub;
    private readonly CallDistributionService _distribution;

    public CallService(AppDbContext db, IHubContext<CallCenterHub> hub, CallDistributionService distribution)
    {
        _db = db;
        _hub = hub;
        _distribution = distribution;
    }

    public async Task<object> GetHistoryAsync(int userId, int page, int pageSize)
    {
        var query = _db.CallRecords
            .Where(c => c.AgentId == userId)
            .Where(c => CallStatuses.FinishedStatuses.Select(s => s.Id).Contains(c.StatusId))
            .OrderByDescending(c => c.StartedAt);

        return await query
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
    }

    public async Task<object> GetActiveAsync(int userId)
    {
        return await _db.CallRecords
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
    }

    public async Task<(int Id, Guid Uid)> StartCallAsync(int userId, StartCallRequest request)
    {
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

        return (call.Id, call.Uid);
    }

    public async Task<(bool Success, string? Error)> HoldCallAsync(int callId)
    {
        var call = await _db.CallRecords.FindAsync(callId);
        if (call == null) return (false, "Arama bulunamadi.");

        call.StatusId = CallStatuses.Ids.OnHold;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> EndCallAsync(int callId, int userId)
    {
        var call = await _db.CallRecords.FindAsync(callId);
        if (call == null) return (false, "Arama bulunamadi.");

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

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AnswerCallAsync(int callId)
    {
        var call = await _db.CallRecords.FindAsync(callId);
        if (call == null) return (false, "Arama bulunamadi.");

        call.StatusId = CallStatuses.Ids.InProgress;
        call.AnsweredAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<object> IncomingCallAsync(IncomingCallRequest request)
    {
        var call = new CallRecord
        {
            CallerNumber = request.CallerNumber,
            CalleeNumber = request.CalleeNumber,
            DirectionId = CallDirections.Ids.Inbound,
            StatusId = CallStatuses.Ids.Ringing,
            StartedAt = DateTime.UtcNow,
            QueueId = request.QueueId
        };

        _db.CallRecords.Add(call);
        await _db.SaveChangesAsync();

        if (request.QueueId.HasValue)
        {
            var assignedAgentId = await _distribution.AssignCallToAgentAsync(request.QueueId.Value, call.Id);
            if (assignedAgentId == null)
            {
                return new { call.Id, call.Uid, Status = "Queued", Message = "Musait agent bulunamadi, kuyrukta bekliyor." };
            }

            return new { call.Id, call.Uid, Status = "Assigned", AgentId = assignedAgentId };
        }

        var notification = new CallNotification
        {
            CallId = call.Id,
            CallerNumber = call.CallerNumber,
            CalleeNumber = call.CalleeNumber,
            DirectionId = call.DirectionId,
            StatusId = CallStatuses.Ids.Ringing
        };

        await _hub.Clients.All.SendAsync("IncomingCall", notification);
        return new { call.Id, call.Uid, Status = "Broadcasting" };
    }
}
