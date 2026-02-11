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

    public async Task<List<CallNotification>> GetActiveAsync(int userId)
    {
        return await _db.CallRecords
            .Where(c => c.AgentId == userId)
            .Where(c => CallStatuses.ActiveStatuses.Select(s => s.Id).Contains(c.StatusId))
            .OrderByDescending(c => c.StartedAt)
            .Select(c => new CallNotification
            {
                CallId = c.Id,
                CallerNumber = c.CallerNumber,
                CalleeNumber = c.CalleeNumber,
                DirectionId = c.DirectionId,
                StatusId = c.StatusId,
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

        // Musteri grubunu bul (agent'in bagli oldugu firma)
        var customerGroupName = await GetCustomerGroupNameAsync(userId);

        // Hub'a bildir — musteri grubu + admin'ler
        await SendToGroupAndAdminsAsync(customerGroupName, "CallEnded", callId);

        // Agent durumunu AfterCallWork yap
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.StatusId = AgentStatuses.Ids.AfterCallWork;
            await _db.SaveChangesAsync();

            await SendToGroupAndAdminsAsync(customerGroupName, "AgentStatusChanged", new AgentStatusUpdate
            {
                AgentId = user.Id,
                AgentName = user.FullName,
                StatusId = AgentStatuses.Ids.AfterCallWork,
                StatusName = AgentStatuses.AfterCallWork.SystemName
            });
        }

        return (true, null);
    }

    public async Task<List<CallNotification>> GetQueuedAsync(int? customerId)
    {
        var query = _db.CallRecords
            .Where(c => c.StatusId == CallStatuses.Ids.Queued);

        // Firma bazli filtreleme
        if (customerId.HasValue && customerId.Value > 0)
        {
            query = query.Where(c => c.Queue != null && c.Queue.CustomerId == customerId.Value);
        }

        return await query
            .OrderBy(c => c.StartedAt) // en eski beklenen once
            .Select(c => new CallNotification
            {
                CallId = c.Id,
                CallerNumber = c.CallerNumber,
                CalleeNumber = c.CalleeNumber,
                DirectionId = c.DirectionId,
                StatusId = c.StatusId,
                QueueName = c.Queue != null ? c.Queue.Name : null
            })
            .ToListAsync();
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
                // Agent bulunamadi — kuyrukta beklet
                call.StatusId = CallStatuses.Ids.Queued;
                await _db.SaveChangesAsync();
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

        // Kuyruk varsa o kuyrugun musterisine, yoksa tum admin'lere bildir
        string? groupName = null;
        if (request.QueueId.HasValue)
        {
            var queueCustomerId = await _db.Queues
                .Where(q => q.Id == request.QueueId.Value)
                .Select(q => q.CustomerId)
                .FirstOrDefaultAsync();
            if (queueCustomerId > 0)
                groupName = $"customer_{queueCustomerId}";
        }

        await SendToGroupAndAdminsAsync(groupName, "IncomingCall", notification);
        return new { call.Id, call.Uid, Status = "Broadcasting" };
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPER METODLAR
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Agent'in bagli oldugu firmanin grup adini bulur.</summary>
    private async Task<string?> GetCustomerGroupNameAsync(int userId)
    {
        var customerId = await _db.CustomerPersonnel
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.CustomerId)
            .FirstOrDefaultAsync();

        return customerId > 0 ? $"customer_{customerId}" : null;
    }

    /// <summary>Musteri grubuna ve admin grubuna ayni anda mesaj gonderir.</summary>
    private async Task SendToGroupAndAdminsAsync(string? groupName, string method, object arg)
    {
        if (groupName != null)
        {
            await _hub.Clients.Group(groupName).SendAsync(method, arg);
        }
        await _hub.Clients.Group("admins").SendAsync(method, arg);
    }
}
