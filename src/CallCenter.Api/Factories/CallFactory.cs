using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Hubs;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class CallFactory : ICallFactory
{
    private readonly ICallRecordEntityService _calls;
    private readonly IQueueEntityService _queues;
    private readonly IUserEntityService _users;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly CallDistributionService _distribution;
    private readonly IHubContext<CallCenterHub> _hub;
    private readonly IUnitOfWork _uow;

    public CallFactory(
        ICallRecordEntityService calls,
        IQueueEntityService queues,
        IUserEntityService users,
        ICustomerPersonnelEntityService personnel,
        CallDistributionService distribution,
        IHubContext<CallCenterHub> hub,
        IUnitOfWork uow)
    {
        _calls = calls;
        _queues = queues;
        _users = users;
        _personnel = personnel;
        _distribution = distribution;
        _hub = hub;
        _uow = uow;
    }

    public async Task<object> GetHistoryAsync(int userId, int page, int pageSize)
    {
        return await _calls.GetAllQueryable()
            .Where(c => c.AgentId == userId)
            .Where(c => CallStatuses.FinishedStatuses.Select(s => s.Id).Contains(c.StatusId))
            .OrderByDescending(c => c.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.Uid,
                c.CallerNumber,
                c.CalleeNumber,
                c.DirectionId,
                c.StatusId,
                c.StartedAt,
                c.DurationSeconds,
                QueueName = c.Queue != null ? c.Queue.Name : null,
                HasRecording = c.CloudFileId != null,
                c.IsRecordingEncrypted,
                c.RecordingFileSize
            })
            .ToListAsync();
    }

    public async Task<List<CallNotification>> GetActiveAsync(int userId)
    {
        return await _calls.GetAllQueryable()
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
        var personnelRecord = await _personnel.GetByUserIdAsync(userId);
        if (personnelRecord?.CustomerRoleId != CustomerRoles.Ids.Operator)
            throw new InvalidOperationException("Bu rolun arama yetkisi yoktur.");

        var call = new CallRecord
        {
            CallerNumber = PhoneHelper.Sanitize(request.CallerNumber),
            CalleeNumber = PhoneHelper.Sanitize(request.CalleeNumber),
            DirectionId = CallDirections.Ids.Outbound,
            StatusId = CallStatuses.Ids.Ringing,
            StartedAt = DateTime.UtcNow,
            AgentId = userId,
            QueueId = request.QueueId
        };

        _calls.Add(call);
        await _uow.SaveChangesAsync();

        var user = await _users.GetByIdAsync(userId);
        if (user != null)
        {
            user.StatusId = AgentStatuses.Ids.InCall;
            await _uow.SaveChangesAsync();
        }

        await BroadcastKpiUpdateAsync(userId);
        return (call.Id, call.Uid);
    }

    public async Task<(bool Success, string? Error)> HoldCallAsync(int callId)
    {
        var call = await _calls.GetByIdAsync(callId);
        if (call == null) return (false, "Arama bulunamadi.");

        call.StatusId = CallStatuses.Ids.OnHold;
        await _uow.SaveChangesAsync();

        if (call.AgentId.HasValue)
            await BroadcastKpiUpdateAsync(call.AgentId.Value);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> EndCallAsync(int callId, int userId)
    {
        var call = await _calls.GetByIdAsync(callId);
        if (call == null) return (false, "Arama bulunamadi.");

        call.StatusId = CallStatuses.Ids.Completed;
        call.EndedAt = DateTime.UtcNow;
        if (call.AnsweredAt.HasValue)
            call.DurationSeconds = (int)(call.EndedAt.Value - call.AnsweredAt.Value).TotalSeconds;
        await _uow.SaveChangesAsync();

        var customerGroupName = await GetCustomerGroupNameAsync(userId);

        await SendToGroupAndAdminsAsync(customerGroupName, "CallEnded", callId);

        var user = await _users.GetByIdAsync(userId);
        if (user != null)
        {
            user.StatusId = AgentStatuses.Ids.AfterCallWork;
            await _uow.SaveChangesAsync();

            await SendToGroupAndAdminsAsync(customerGroupName, "AgentStatusChanged", new AgentStatusUpdate
            {
                AgentId = user.Id,
                AgentName = user.FullName,
                StatusId = AgentStatuses.Ids.AfterCallWork,
                StatusName = AgentStatuses.AfterCallWork.SystemName
            });
        }

        await BroadcastKpiUpdateAsync(userId);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AnswerCallAsync(int callId)
    {
        var call = await _calls.GetByIdAsync(callId);
        if (call == null) return (false, "Arama bulunamadi.");

        call.StatusId = CallStatuses.Ids.InProgress;
        call.AnsweredAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        if (call.AgentId.HasValue)
            await BroadcastKpiUpdateAsync(call.AgentId.Value);

        return (true, null);
    }

    public async Task<List<CallNotification>> GetQueuedAsync(int? customerId)
    {
        var query = _calls.GetAllQueryable()
            .Where(c => c.StatusId == CallStatuses.Ids.Queued);

        if (customerId.HasValue && customerId.Value > 0)
            query = query.Where(c => c.Queue != null && c.Queue.CustomerId == customerId.Value);

        return await query
            .OrderBy(c => c.StartedAt)
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

        _calls.Add(call);
        await _uow.SaveChangesAsync();

        if (request.QueueId.HasValue)
        {
            var assignedAgentId = await _distribution.AssignCallToAgentAsync(request.QueueId.Value, call.Id);
            if (assignedAgentId == null)
            {
                call.StatusId = CallStatuses.Ids.Queued;
                await _uow.SaveChangesAsync();
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

        string? groupName = null;
        if (request.QueueId.HasValue)
        {
            var queueCustomerId = await _queues.GetAllQueryable()
                .Where(q => q.Id == request.QueueId.Value)
                .Select(q => q.CustomerId)
                .FirstOrDefaultAsync();
            if (queueCustomerId > 0)
                groupName = $"customer_{queueCustomerId}";
        }

        await SendToGroupAndAdminsAsync(groupName, "IncomingCall", notification);

        // KPI broadcast — IncomingCall icin groupName'den customer bilgisi kullan
        try
        {
            var activeStatusIds = CallStatuses.ActiveStatuses.Select(s => s.Id).ToList();
            var kpi = new DashboardKpiUpdate
            {
                ActiveCallCount = await _calls.GetAllQueryable().Where(c => activeStatusIds.Contains(c.StatusId)).CountAsync(),
                QueueWaitingCount = await _calls.GetAllQueryable().Where(c => c.StatusId == CallStatuses.Ids.Queued || c.StatusId == CallStatuses.Ids.Ringing).CountAsync(),
                AvailableAgentCount = await _users.GetAllQueryable().Where(u => u.IsActive && (u.RoleId == UserRoles.Ids.Agent || u.RoleId == UserRoles.Ids.CustomerUser) && u.StatusId == AgentStatuses.Ids.Available).CountAsync()
            };
            await SendToGroupAndAdminsAsync(groupName, "DashboardKpiUpdated", kpi);
        }
        catch { }

        return new { call.Id, call.Uid, Status = "Broadcasting" };
    }

    public async Task<CallSyncPushResponse> SyncPushAsync(int userId, CallSyncPushRequest request)
    {
        var existing = await _calls.GetByUidAsync(request.Uid);

        if (existing != null)
        {
            existing.StatusId = MapStatusNameToId(request.Status);
            existing.AnsweredAt = request.AnsweredAt;
            existing.EndedAt = request.EndedAt;
            existing.DurationSeconds = request.DurationSeconds;
            existing.Notes = request.Notes;
            if (!string.IsNullOrEmpty(request.RecordingFilePath))
                existing.RecordingFilePath = request.RecordingFilePath;
            if (!string.IsNullOrEmpty(request.RecordingFileHash))
                existing.RecordingFileHash = request.RecordingFileHash;
            if (request.RecordingFileSize.HasValue)
                existing.RecordingFileSize = request.RecordingFileSize;
            existing.IsRecordingEncrypted = request.IsRecordingEncrypted;
            if (request.RecordingRetentionDate.HasValue)
                existing.RecordingRetentionDate = request.RecordingRetentionDate;
            if (!string.IsNullOrEmpty(request.CloudFileId))
                existing.CloudFileId = request.CloudFileId;
            if (!string.IsNullOrEmpty(request.CloudFileName))
                existing.CloudFileName = request.CloudFileName;
            if (request.CloudUploadedAt.HasValue)
                existing.CloudUploadedAt = request.CloudUploadedAt;
            if (!string.IsNullOrEmpty(request.PlatformFileId))
                existing.PlatformFileId = request.PlatformFileId;
            if (request.PlatformUploadedAt.HasValue)
                existing.PlatformUploadedAt = request.PlatformUploadedAt;
            await _uow.SaveChangesAsync();

            return new CallSyncPushResponse { Id = existing.Id, IsNew = false };
        }

        var call = new CallRecord
        {
            Uid = request.Uid,
            CallerNumber = request.CallerNumber,
            CalleeNumber = request.CalleeNumber,
            DirectionId = request.Direction == "Inbound"
                ? CallDirections.Ids.Inbound
                : CallDirections.Ids.Outbound,
            StatusId = MapStatusNameToId(request.Status),
            StartedAt = request.StartedAt,
            AnsweredAt = request.AnsweredAt,
            EndedAt = request.EndedAt,
            DurationSeconds = request.DurationSeconds,
            AgentId = userId,
            Notes = request.Notes,
            RecordingFilePath = request.RecordingFilePath,
            RecordingFileHash = request.RecordingFileHash,
            RecordingFileSize = request.RecordingFileSize,
            IsRecordingEncrypted = request.IsRecordingEncrypted,
            RecordingRetentionDate = request.RecordingRetentionDate,
            CloudFileId = request.CloudFileId,
            CloudFileName = request.CloudFileName,
            CloudUploadedAt = request.CloudUploadedAt,
            PlatformFileId = request.PlatformFileId,
            PlatformUploadedAt = request.PlatformUploadedAt
        };

        if (!string.IsNullOrEmpty(request.QueueName))
        {
            var queue = await _queues.GetAllQueryable()
                .FirstOrDefaultAsync(q => q.Name == request.QueueName);
            if (queue != null) call.QueueId = queue.Id;
        }

        _calls.Add(call);
        await _uow.SaveChangesAsync();

        return new CallSyncPushResponse { Id = call.Id, IsNew = true };
    }

    public async Task<MyStatsResponse> GetMyStatsAsync(int userId)
    {
        var today = DateTime.UtcNow.Date;

        var todayCalls = await _calls.GetAllQueryable()
            .Where(c => c.AgentId == userId && c.StartedAt >= today)
            .Select(c => new { c.StatusId, c.DirectionId, c.DurationSeconds })
            .ToListAsync();

        var answered = todayCalls.Where(c => c.StatusId == CallStatuses.Ids.Completed).ToList();
        var totalDuration = answered.Sum(c => c.DurationSeconds);

        var recentCallsRaw = await _calls.GetAllQueryable()
            .Where(c => c.AgentId == userId)
            .OrderByDescending(c => c.StartedAt)
            .Take(15)
            .Select(c => new
            {
                c.Id, c.CallerNumber, c.CalleeNumber,
                c.DirectionId, c.StatusId, c.StartedAt, c.DurationSeconds,
                c.AgentId,
                AgentName = c.Agent != null ? c.Agent.FullName : null,
                QueueName = c.Queue != null ? c.Queue.Name : null
            })
            .ToListAsync();

        return new MyStatsResponse
        {
            TodayTotalCalls = todayCalls.Count,
            TodayAnsweredCalls = answered.Count,
            TodayMissedCalls = todayCalls.Count(c => c.StatusId == CallStatuses.Ids.Missed),
            TodayOutboundCalls = todayCalls.Count(c => c.DirectionId == CallDirections.Ids.Outbound),
            TodayInboundCalls = todayCalls.Count(c => c.DirectionId == CallDirections.Ids.Inbound),
            TodayTotalDurationSeconds = totalDuration,
            TodayAvgDurationSeconds = answered.Count > 0 ? totalDuration / answered.Count : 0,
            RecentCalls = recentCallsRaw.Select(c => new RecentCallDto
            {
                Id = c.Id,
                CallerNumber = c.CallerNumber,
                CalleeNumber = c.CalleeNumber,
                DirectionId = c.DirectionId,
                DirectionName = CallDirections.GetById(c.DirectionId)?.SystemName ?? "Unknown",
                StatusId = c.StatusId,
                StatusName = CallStatuses.GetById(c.StatusId)?.SystemName ?? "Unknown",
                StartedAt = c.StartedAt,
                DurationSeconds = c.DurationSeconds,
                AgentId = c.AgentId,
                AgentName = c.AgentName,
                QueueName = c.QueueName
            }).ToList()
        };
    }

    public async Task<(bool Success, string? Error)> UpdateNotesAsync(int callId, string? notes)
    {
        var call = await _calls.GetByIdAsync(callId);
        if (call == null) return (false, "Arama bulunamadi.");

        call.Notes = notes;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static int MapStatusNameToId(string statusName)
    {
        var status = CallStatuses.GetBySystemName(statusName);
        return status?.Id ?? CallStatuses.Ids.Completed;
    }

    private async Task<string?> GetCustomerGroupNameAsync(int userId)
    {
        var customerId = await _personnel.GetAllQueryable()
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.CustomerId)
            .FirstOrDefaultAsync();

        return customerId > 0 ? $"customer_{customerId}" : null;
    }

    /// <summary>
    /// Arama durumu degistiginde Dashboard KPI guncellemesini broadcast eder.
    /// </summary>
    private async Task BroadcastKpiUpdateAsync(int userId)
    {
        try
        {
            var customerGroupName = await GetCustomerGroupNameAsync(userId);

            List<int>? customerAgentIds = null;
            if (customerGroupName != null)
            {
                var cidStr = customerGroupName.Replace("customer_", "");
                if (int.TryParse(cidStr, out var cid))
                {
                    customerAgentIds = await _personnel.GetAllQueryable()
                        .Where(cp => cp.CustomerId == cid)
                        .Select(cp => cp.UserId)
                        .ToListAsync();
                }
            }

            var activeStatusIds = CallStatuses.ActiveStatuses.Select(s => s.Id).ToList();

            var activeCallCount = await _calls.GetAllQueryable()
                .Where(c => activeStatusIds.Contains(c.StatusId))
                .Where(c => customerAgentIds == null || (c.AgentId.HasValue && customerAgentIds.Contains(c.AgentId.Value)))
                .CountAsync();

            var queueWaitingCount = await _calls.GetAllQueryable()
                .Where(c => c.StatusId == CallStatuses.Ids.Queued || c.StatusId == CallStatuses.Ids.Ringing)
                .Where(c => customerAgentIds == null || (c.AgentId.HasValue && customerAgentIds.Contains(c.AgentId.Value)))
                .CountAsync();

            var availableAgentCount = await _users.GetAllQueryable()
                .Where(u => u.IsActive && (u.RoleId == UserRoles.Ids.Agent || u.RoleId == UserRoles.Ids.CustomerUser) && u.StatusId == AgentStatuses.Ids.Available)
                .Where(u => customerAgentIds == null || customerAgentIds.Contains(u.Id))
                .CountAsync();

            var today = DateTime.UtcNow.Date;
            var todayCalls = await _calls.GetAllQueryable()
                .Where(c => c.StartedAt >= today)
                .Where(c => customerAgentIds == null || (c.AgentId.HasValue && customerAgentIds.Contains(c.AgentId.Value)))
                .Select(c => c.StatusId)
                .ToListAsync();

            var kpi = new DashboardKpiUpdate
            {
                ActiveCallCount = activeCallCount,
                AvailableAgentCount = availableAgentCount,
                QueueWaitingCount = queueWaitingCount,
                TodayTotalCallCount = todayCalls.Count,
                TodayAnsweredCount = todayCalls.Count(s => s == CallStatuses.Ids.Completed),
                TodayMissedCount = todayCalls.Count(s => s == CallStatuses.Ids.Missed)
            };

            await SendToGroupAndAdminsAsync(customerGroupName, "DashboardKpiUpdated", kpi);
        }
        catch (Exception ex)
        {
            // KPI broadcast hatasi arama islemini engellemeyecek
            Console.WriteLine($"[CallFactory] KPI broadcast hatasi: {ex.Message}");
        }
    }

    private async Task SendToGroupAndAdminsAsync(string? groupName, string method, object arg)
    {
        if (groupName != null)
            await _hub.Clients.Group(groupName).SendAsync(method, arg);
        await _hub.Clients.Group("admins").SendAsync(method, arg);
    }
}
