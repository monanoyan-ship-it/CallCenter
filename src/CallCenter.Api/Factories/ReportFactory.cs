using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class ReportFactory : IReportFactory
{
    private readonly ICallRecordEntityService _callEs;
    private readonly ICustomerPersonnelEntityService _personnelEs;
    private readonly IUserEntityService _userEs;

    public ReportFactory(ICallRecordEntityService callEs, ICustomerPersonnelEntityService personnelEs, IUserEntityService userEs)
    {
        _callEs = callEs;
        _personnelEs = personnelEs;
        _userEs = userEs;
    }

    public async Task<CallReportResponse> GetCallReportAsync(int? customerId, DateTime? from, DateTime? to, int? directionId, int? statusId, int page, int pageSize)
    {
        List<int>? customerAgentIds = null;
        if (customerId.HasValue && customerId.Value > 0)
        {
            customerAgentIds = await _personnelEs.GetAllQueryable()
                .Where(cp => cp.CustomerId == customerId.Value)
                .Select(cp => cp.UserId)
                .ToListAsync();
        }

        var query = _callEs.GetAllQueryable();

        if (customerAgentIds != null)
            query = query.Where(c => c.AgentId.HasValue && customerAgentIds.Contains(c.AgentId.Value));

        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc);
            query = query.Where(c => c.StartedAt >= fromUtc);
        }
        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(c => c.StartedAt < toUtc);
        }

        if (directionId.HasValue && directionId.Value > 0)
            query = query.Where(c => c.DirectionId == directionId.Value);

        if (statusId.HasValue && statusId.Value > 0)
            query = query.Where(c => c.StatusId == statusId.Value);

        var totalCalls = await query.CountAsync();
        var answeredCalls = await query.CountAsync(c => c.StatusId == CallStatuses.Ids.Completed);
        var missedCalls = await query.CountAsync(c => c.StatusId == CallStatuses.Ids.Missed);
        var avgDuration = totalCalls > 0
            ? (int)await query.Where(c => c.StatusId == CallStatuses.Ids.Completed && c.DurationSeconds > 0)
                              .Select(c => (double)c.DurationSeconds)
                              .DefaultIfEmpty(0)
                              .AverageAsync()
            : 0;

        var itemsRaw = await query
            .OrderByDescending(c => c.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.CallerNumber,
                c.CalleeNumber,
                c.DirectionId,
                c.StatusId,
                c.DurationSeconds,
                c.StartedAt,
                AgentName = c.Agent != null ? c.Agent.FullName : null,
                QueueName = c.Queue != null ? c.Queue.Name : null
            })
            .ToListAsync();

        var items = itemsRaw.Select(c => new CallReportItemDto
        {
            Id = c.Id,
            CallerNumber = c.CallerNumber,
            CalleeNumber = c.CalleeNumber,
            DirectionId = c.DirectionId,
            DirectionName = CallDirections.GetById(c.DirectionId)?.SystemName ?? "Unknown",
            StatusId = c.StatusId,
            StatusName = CallStatuses.GetById(c.StatusId)?.SystemName ?? "Unknown",
            DurationSeconds = c.DurationSeconds,
            StartedAt = c.StartedAt,
            AgentName = c.AgentName,
            QueueName = c.QueueName
        }).ToList();

        return new CallReportResponse
        {
            TotalCalls = totalCalls,
            AnsweredCalls = answeredCalls,
            MissedCalls = missedCalls,
            AvgDurationSeconds = avgDuration,
            Items = new PagedResult<CallReportItemDto>
            {
                Items = items,
                TotalCount = totalCalls,
                Page = page,
                PageSize = pageSize
            }
        };
    }

    public async Task<AgentReportResponse> GetAgentReportAsync(int? customerId, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var agentsQuery = _userEs.GetAllQueryable().Where(u => u.IsActive && u.RoleId == UserRoles.Ids.Agent);

        if (customerId.HasValue && customerId.Value > 0)
        {
            var customerAgentIds = await _personnelEs.GetAllQueryable()
                .Where(cp => cp.CustomerId == customerId.Value)
                .Select(cp => cp.UserId)
                .ToListAsync();

            agentsQuery = agentsQuery.Where(u => customerAgentIds.Contains(u.Id));
        }

        var callsQuery = _callEs.GetAllQueryable();
        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc);
            callsQuery = callsQuery.Where(c => c.StartedAt >= fromUtc);
        }
        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc);
            callsQuery = callsQuery.Where(c => c.StartedAt < toUtc);
        }

        var totalAgents = await agentsQuery.CountAsync();

        var agentStats = await agentsQuery
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Extension,
                TotalCalls = callsQuery.Count(c => c.AgentId == u.Id),
                AnsweredCalls = callsQuery.Count(c => c.AgentId == u.Id && c.StatusId == CallStatuses.Ids.Completed),
                MissedCalls = callsQuery.Count(c => c.AgentId == u.Id && c.StatusId == CallStatuses.Ids.Missed),
                AvgDuration = callsQuery
                    .Where(c => c.AgentId == u.Id && c.StatusId == CallStatuses.Ids.Completed && c.DurationSeconds > 0)
                    .Select(c => (double?)c.DurationSeconds)
                    .Average()
            })
            .ToListAsync();

        var items = agentStats.Select(a => new AgentReportItemDto
        {
            AgentId = a.Id,
            FullName = a.FullName,
            Extension = a.Extension,
            TotalCalls = a.TotalCalls,
            AnsweredCalls = a.AnsweredCalls,
            MissedCalls = a.MissedCalls,
            AvgDurationSeconds = (int)(a.AvgDuration ?? 0),
            AnswerRate = a.TotalCalls > 0 ? Math.Round((double)a.AnsweredCalls / a.TotalCalls * 100, 1) : 0
        }).ToList();

        var bestPerformer = items.OrderByDescending(i => i.AnswerRate).ThenByDescending(i => i.TotalCalls).FirstOrDefault();

        var overallAvg = items.Count > 0 && items.Any(i => i.AvgDurationSeconds > 0)
            ? (int)items.Where(i => i.AvgDurationSeconds > 0).Average(i => i.AvgDurationSeconds)
            : 0;

        var lowestAvg = items.Count > 0 && items.Any(i => i.AvgDurationSeconds > 0)
            ? items.Where(i => i.AvgDurationSeconds > 0).Min(i => i.AvgDurationSeconds)
            : 0;

        return new AgentReportResponse
        {
            TotalAgents = totalAgents,
            BestPerformerName = bestPerformer?.FullName,
            LowestAvgAnswerSeconds = lowestAvg,
            OverallAvgDurationSeconds = overallAvg,
            Items = new PagedResult<AgentReportItemDto>
            {
                Items = items,
                TotalCount = totalAgents,
                Page = page,
                PageSize = pageSize
            }
        };
    }
}
