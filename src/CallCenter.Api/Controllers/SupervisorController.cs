using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class SupervisorController : ControllerBase
{
    private readonly AppDbContext _db;

    public SupervisorController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Supervisor dashboard verilerini tek seferde dondurur.
    /// customerId verilirse sadece o firmanin verileri, verilmezse tum sistem.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard([FromQuery] int? customerId)
    {
        var today = DateTime.UtcNow.Date;
        var activeStatusIds = CallStatuses.ActiveStatuses.Select(s => s.Id).ToList();

        // ─── Firma bazli filtreleme icin agent ID'lerini bul ───
        List<int>? customerAgentIds = null;
        if (customerId.HasValue && customerId.Value > 0)
        {
            customerAgentIds = await _db.CustomerPersonnel
                .Where(cp => cp.CustomerId == customerId.Value)
                .Select(cp => cp.UserId)
                .ToListAsync();
        }

        // ─── AGENTS ───
        var agentsQuery = _db.Users.Where(u => u.IsActive);
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

        // Aktif aramalari bul (agent bazli current call)
        var agentIds = agents.Select(a => a.Id).ToList();
        var activeCalls = await _db.CallRecords
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

        // ─── CALLS — Bugunun aramalari ───
        var callsQuery = _db.CallRecords.Where(c => c.StartedAt >= today);
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

        // Aktif arama sayisi
        var activeCallCount = await _db.CallRecords
            .Where(c => activeStatusIds.Contains(c.StatusId))
            .Where(c => customerAgentIds == null || (c.AgentId.HasValue && customerAgentIds.Contains(c.AgentId.Value)))
            .CountAsync();

        // Kuyrukta bekleyen (Ringing durumunda olan aramalar)
        var queueWaitingCount = await _db.CallRecords
            .Where(c => c.StatusId == CallStatuses.Ids.Ringing)
            .Where(c => customerAgentIds == null || (c.AgentId.HasValue && customerAgentIds.Contains(c.AgentId.Value)))
            .CountAsync();

        // Musait agent sayisi
        var availableAgentCount = agentDtos.Count(a => a.StatusId == AgentStatuses.Ids.Available);

        // ─── RECENT CALLS — Son 10 arama ───
        var recentCallsBaseQuery = _db.CallRecords.AsQueryable();

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

        // ─── QUEUES (firma bazli filtreleme) ───
        var queuesQuery = _db.Queues.Where(q => q.IsActive);
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

        return Ok(new DashboardResponse
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
        });
    }

    /// <summary>
    /// Canli kuyruk izleme — her kuyrugun anlik durumu + agent listesi.
    /// </summary>
    [HttpGet("queues/live")]
    public async Task<ActionResult<List<QueueLiveDto>>> GetQueuesLive([FromQuery] int? customerId)
    {
        var queuesQuery = _db.Queues.Where(q => q.IsActive);
        if (customerId.HasValue && customerId.Value > 0)
            queuesQuery = queuesQuery.Where(q => q.CustomerId == customerId.Value);

        var queues = await queuesQuery
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

        return Ok(queues);
    }

    /// <summary>
    /// Dashboard'daki firma dropdown icin musteri listesi.
    /// </summary>
    [HttpGet("customers")]
    public async Task<ActionResult<List<CustomerSimpleDto>>> GetCustomers()
    {
        var customers = await _db.Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CustomerSimpleDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync();

        return Ok(customers);
    }
}
