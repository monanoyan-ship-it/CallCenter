using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SupervisorFactory : ISupervisorFactory
{
    private readonly IUserEntityService _userEs;
    private readonly ICustomerPersonnelEntityService _personnelEs;
    private readonly ICallRecordEntityService _callEs;
    private readonly IQueueEntityService _queueEs;
    private readonly ICustomerEntityService _customerEs;

    public SupervisorFactory(
        IUserEntityService userEs,
        ICustomerPersonnelEntityService personnelEs,
        ICallRecordEntityService callEs,
        IQueueEntityService queueEs,
        ICustomerEntityService customerEs)
    {
        _userEs = userEs;
        _personnelEs = personnelEs;
        _callEs = callEs;
        _queueEs = queueEs;
        _customerEs = customerEs;
    }

    public async Task<DashboardResponse> GetDashboardAsync(int? customerId)
    {
        var today = DateTime.UtcNow.Date;
        var activeStatusIds = CallStatuses.ActiveStatuses.Select(s => s.Id).ToList();

        List<int>? customerAgentIds = null;
        if (customerId.HasValue && customerId.Value > 0)
        {
            customerAgentIds = await _personnelEs.GetAllQueryable()
                .Where(cp => cp.CustomerId == customerId.Value)
                .Select(cp => cp.UserId)
                .ToListAsync();
        }

        // AGENTS — sadece Agent rolundeki kullanicilar (operatorler)
        var agentsQuery = _userEs.GetAllQueryable()
            .Where(u => u.IsActive && u.RoleId == UserRoles.Ids.Agent);
        if (customerAgentIds != null)
        {
            agentsQuery = agentsQuery.Where(u => customerAgentIds.Contains(u.Id));
        }

        var agents = await agentsQuery
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Extension,
                u.RoleId,
                u.StatusId
            })
            .ToListAsync();

        var agentIds = agents.Select(a => a.Id).ToList();
        var activeCalls = await _callEs.GetAllQueryable()
            .Where(c => activeStatusIds.Contains(c.StatusId) && c.AgentId.HasValue && agentIds.Contains(c.AgentId.Value))
            .Select(c => new { c.AgentId, c.Id })
            .ToListAsync();

        var agentCurrentCallMap = activeCalls
            .GroupBy(c => c.AgentId!.Value)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var agentDtos = agents.Select(a =>
        {
            var role = UserRoles.GetById(a.RoleId);
            var status = AgentStatuses.GetById(a.StatusId);
            return new AgentStatusDto
            {
                Id = a.Id,
                FullName = a.FullName,
                Extension = a.Extension,
                RoleId = a.RoleId,
                RoleName = role?.SystemName ?? "Agent",
                StatusId = a.StatusId,
                StatusName = status?.SystemName ?? "Offline",
                StatusCss = status?.CssClass ?? "offline",
                StatusIcon = status?.Icon ?? "bi-circle-fill",
                CurrentCallId = agentCurrentCallMap.GetValueOrDefault(a.Id)
            };
        }).ToList();

        // CALLS
        var callsQuery = _callEs.GetAllQueryable().Where(c => c.StartedAt >= today);
        if (customerAgentIds != null)
        {
            callsQuery = callsQuery.Where(c => c.AgentId.HasValue && customerAgentIds.Contains(c.AgentId.Value));
        }

        var todayCalls = await callsQuery
            .Select(c => new
            {
                c.Id,
                c.StatusId,
                c.DurationSeconds,
                c.AgentId
            })
            .ToListAsync();

        var todayTotal = todayCalls.Count;
        var todayAnswered = todayCalls.Count(c => c.StatusId == CallStatuses.Ids.Completed);
        var todayMissed = todayCalls.Count(c => c.StatusId == CallStatuses.Ids.Missed);
        var answeredCalls = todayCalls.Where(c => c.StatusId == CallStatuses.Ids.Completed && c.DurationSeconds > 0).ToList();
        var todayAvgDuration = answeredCalls.Count > 0 ? (int)answeredCalls.Average(c => c.DurationSeconds) : 0;

        var activeCallCount = await _callEs.GetAllQueryable()
            .Where(c => activeStatusIds.Contains(c.StatusId))
            .Where(c => customerAgentIds == null || (c.AgentId.HasValue && customerAgentIds.Contains(c.AgentId.Value)))
            .CountAsync();

        var queueWaitingCount = await _callEs.GetAllQueryable()
            .Where(c => c.StatusId == CallStatuses.Ids.Ringing)
            .Where(c => customerAgentIds == null || (c.AgentId.HasValue && customerAgentIds.Contains(c.AgentId.Value)))
            .CountAsync();

        var availableAgentCount = agentDtos.Count(a => a.StatusId == AgentStatuses.Ids.Available);

        // RECENT CALLS
        var recentCallsBaseQuery = _callEs.GetAllQueryable();

        if (customerAgentIds != null)
        {
            recentCallsBaseQuery = recentCallsBaseQuery
                .Where(c => c.AgentId.HasValue && customerAgentIds.Contains(c.AgentId.Value));
        }

        var recentCallsRaw = await recentCallsBaseQuery
            .OrderByDescending(c => c.StartedAt)
            .Take(10)
            .Select(c => new
            {
                c.Id,
                c.CallerNumber,
                c.CalleeNumber,
                c.DirectionId,
                c.StatusId,
                c.StartedAt,
                c.DurationSeconds,
                c.AgentId,
                AgentName = c.Agent != null ? c.Agent.FullName : null,
                QueueName = c.Queue != null ? c.Queue.Name : null
            })
            .ToListAsync();

        var recentCalls = recentCallsRaw.Select(c => new RecentCallDto
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
        }).ToList();

        // QUEUES
        var queuesQuery = _queueEs.GetAllQueryable().Where(q => q.IsActive);
        if (customerId.HasValue && customerId.Value > 0)
        {
            queuesQuery = queuesQuery.Where(q => q.CustomerId == customerId.Value);
        }

        var queues = await queuesQuery
            .Select(q => new QueueSummaryDto
            {
                Id = q.Id,
                Name = q.Name,
                IsActive = q.IsActive,
                AgentCount = q.QueueAgents.Count,
                WaitingCount = q.CallRecords.Count(c => c.StatusId == CallStatuses.Ids.Ringing),
                ActiveCount = q.CallRecords.Count(c => c.StatusId == CallStatuses.Ids.InProgress || c.StatusId == CallStatuses.Ids.OnHold)
            })
            .ToListAsync();

        return new DashboardResponse
        {
            ActiveCallCount = activeCallCount,
            AvailableAgentCount = availableAgentCount,
            QueueWaitingCount = queueWaitingCount,
            TodayTotalCallCount = todayTotal,
            TodayAnsweredCount = todayAnswered,
            TodayMissedCount = todayMissed,
            TodayAvgDurationSeconds = todayAvgDuration,
            Agents = agentDtos,
            RecentCalls = recentCalls,
            QueueSummaries = queues
        };
    }

    public async Task<List<QueueLiveDto>> GetQueuesLiveAsync(int? customerId)
    {
        var queuesQuery = _queueEs.GetAllQueryable().Where(q => q.IsActive);
        if (customerId.HasValue && customerId.Value > 0)
            queuesQuery = queuesQuery.Where(q => q.CustomerId == customerId.Value);

        return await queuesQuery
            .OrderBy(q => q.Customer.Name).ThenBy(q => q.Name)
            .Select(q => new QueueLiveDto
            {
                Id = q.Id,
                Name = q.Name,
                CustomerName = q.Customer.Name,
                WaitingCount = q.CallRecords.Count(c => c.StatusId == CallStatuses.Ids.Ringing),
                ActiveCount = q.CallRecords.Count(c => c.StatusId == CallStatuses.Ids.InProgress || c.StatusId == CallStatuses.Ids.OnHold),
                Agents = q.QueueAgents.Select(qa => new QueueLiveAgentDto
                {
                    Id = qa.Agent.Id,
                    FullName = qa.Agent.FullName,
                    Extension = qa.Agent.Extension,
                    StatusId = qa.Agent.StatusId
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<List<CustomerSimpleDto>> GetCustomersAsync()
    {
        return await _customerEs.GetAllQueryable()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CustomerSimpleDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync();
    }
}
