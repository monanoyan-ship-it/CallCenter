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
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Arama raporlari — ozet istatistikler + sayfalamali arama listesi.
    /// </summary>
    [HttpGet("calls")]
    public async Task<ActionResult<CallReportResponse>> GetCallReport(
        [FromQuery] int? customerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? directionId,
        [FromQuery] int? statusId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Firma bazli agent ID'leri
        List<int>? customerAgentIds = null;
        if (customerId.HasValue && customerId.Value > 0)
        {
            customerAgentIds = await _db.CustomerPersonnel
                .Where(cp => cp.CustomerId == customerId.Value)
                .Select(cp => cp.UserId)
                .ToListAsync();
        }

        var query = _db.CallRecords.AsQueryable();

        // Firma filtresi
        if (customerAgentIds != null)
            query = query.Where(c => c.AgentId.HasValue && customerAgentIds.Contains(c.AgentId.Value));

        // Tarih filtresi
        if (from.HasValue)
            query = query.Where(c => c.StartedAt >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(c => c.StartedAt < to.Value.Date.AddDays(1));

        // Yon filtresi
        if (directionId.HasValue && directionId.Value > 0)
            query = query.Where(c => c.DirectionId == directionId.Value);

        // Durum filtresi
        if (statusId.HasValue && statusId.Value > 0)
            query = query.Where(c => c.StatusId == statusId.Value);

        // Ozet istatistikler (filtrelenmic veri uzerinden)
        var totalCalls = await query.CountAsync();
        var answeredCalls = await query.CountAsync(c => c.StatusId == CallStatuses.Ids.Completed);
        var missedCalls = await query.CountAsync(c => c.StatusId == CallStatuses.Ids.Missed);
        var avgDuration = totalCalls > 0
            ? (int)await query.Where(c => c.StatusId == CallStatuses.Ids.Completed && c.DurationSeconds > 0)
                              .Select(c => (double)c.DurationSeconds)
                              .DefaultIfEmpty(0)
                              .AverageAsync()
            : 0;

        // Sayfalamali liste
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

        return Ok(new CallReportResponse
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
        });
    }

    /// <summary>
    /// Temsilci performans raporu — ozet + sayfalamali temsilci listesi.
    /// </summary>
    [HttpGet("agents")]
    public async Task<ActionResult<AgentReportResponse>> GetAgentReport(
        [FromQuery] int? customerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Agent'lari belirle
        var agentsQuery = _db.Users.Where(u => u.IsActive && u.RoleId == UserRoles.Ids.Agent);

        if (customerId.HasValue && customerId.Value > 0)
        {
            var customerAgentIds = await _db.CustomerPersonnel
                .Where(cp => cp.CustomerId == customerId.Value)
                .Select(cp => cp.UserId)
                .ToListAsync();

            agentsQuery = agentsQuery.Where(u => customerAgentIds.Contains(u.Id));
        }

        // Tarih filtreli arama sorgusu
        var callsQuery = _db.CallRecords.AsQueryable();
        if (from.HasValue)
            callsQuery = callsQuery.Where(c => c.StartedAt >= from.Value.Date);
        if (to.HasValue)
            callsQuery = callsQuery.Where(c => c.StartedAt < to.Value.Date.AddDays(1));

        // Agent bazli performans metrikleri
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

        // Ozet: en iyi performans (en yuksek cevaplama orani)
        var bestPerformer = items.OrderByDescending(i => i.AnswerRate).ThenByDescending(i => i.TotalCalls).FirstOrDefault();

        // Genel ort. sure
        var overallAvg = items.Count > 0 && items.Any(i => i.AvgDurationSeconds > 0)
            ? (int)items.Where(i => i.AvgDurationSeconds > 0).Average(i => i.AvgDurationSeconds)
            : 0;

        // En dusuk ort. cevaplama suresi (en hizli agent)
        var lowestAvg = items.Count > 0 && items.Any(i => i.AvgDurationSeconds > 0)
            ? items.Where(i => i.AvgDurationSeconds > 0).Min(i => i.AvgDurationSeconds)
            : 0;

        return Ok(new AgentReportResponse
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
        });
    }
}
