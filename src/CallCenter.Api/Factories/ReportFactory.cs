using System.Text;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class ReportFactory : IReportFactory
{
    private readonly ICallRecordEntityService _callEs;
    private readonly ICustomerPersonnelEntityService _personnelEs;
    private readonly IUserEntityService _userEs;
    private readonly IQueueEntityService _queueEs;

    public ReportFactory(ICallRecordEntityService callEs, ICustomerPersonnelEntityService personnelEs, IUserEntityService userEs, IQueueEntityService queueEs)
    {
        _callEs = callEs;
        _personnelEs = personnelEs;
        _userEs = userEs;
        _queueEs = queueEs;
    }

    // ═══════════════════════════════════════════════════════════════
    // ORTAK FILTRE
    // ═══════════════════════════════════════════════════════════════

    private async Task<IQueryable<Shared.Entities.CallRecord>> BuildFilteredQueryAsync(int? customerId, DateTime? from, DateTime? to, int? directionId = null, int? statusId = null)
    {
        var query = _callEs.GetAllQueryable();

        if (customerId.HasValue && customerId.Value > 0)
        {
            var customerAgentIds = await _personnelEs.GetAllQueryable()
                .Where(cp => cp.CustomerId == customerId.Value)
                .Select(cp => cp.UserId)
                .ToListAsync();
            query = query.Where(c => c.AgentId.HasValue && customerAgentIds.Contains(c.AgentId.Value));
        }

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

        return query;
    }

    // ═══════════════════════════════════════════════════════════════
    // ARAMA RAPORU (SLA metrikleri eklendi)
    // ═══════════════════════════════════════════════════════════════

    public async Task<CallReportResponse> GetCallReportAsync(int? customerId, DateTime? from, DateTime? to, int? directionId, int? statusId, int page, int pageSize)
    {
        var query = await BuildFilteredQueryAsync(customerId, from, to, directionId, statusId);

        var totalCalls = await query.CountAsync();
        var answeredCalls = await query.CountAsync(c => c.StatusId == CallStatuses.Ids.Completed);
        var missedCalls = await query.CountAsync(c => c.StatusId == CallStatuses.Ids.Missed);
        var completedDurations = await query
            .Where(c => c.StatusId == CallStatuses.Ids.Completed && c.DurationSeconds > 0)
            .Select(c => c.DurationSeconds)
            .ToListAsync();
        var avgDuration = completedDurations.Count > 0 ? (int)completedDurations.Average() : 0;

        // SLA metrikleri
        var abandonmentRate = totalCalls > 0 ? Math.Round((double)missedCalls / totalCalls * 100, 1) : 0;

        var answeredWithTimes = await query
            .Where(c => c.StatusId == CallStatuses.Ids.Completed && c.AnsweredAt.HasValue)
            .Select(c => new
            {
                c.StartedAt,
                AnsweredAt = c.AnsweredAt!.Value,
                c.EndedAt,
                MaxWait = c.QueueId.HasValue ? c.Queue!.MaxWaitTimeSeconds : 300
            })
            .ToListAsync();

        var asa = answeredWithTimes.Count > 0
            ? (int)answeredWithTimes.Average(x => (x.AnsweredAt - x.StartedAt).TotalSeconds)
            : 0;
        var aht = answeredWithTimes.Count > 0
            ? (int)answeredWithTimes.Where(x => x.EndedAt.HasValue).Select(x => (x.EndedAt!.Value - x.AnsweredAt).TotalSeconds).DefaultIfEmpty(0).Average()
            : 0;

        // SLA: kuyruk bazli MaxWaitTime'a gore hesaplama
        double slaRate = 0;
        if (answeredWithTimes.Count > 0)
        {
            var slaCompliant = answeredWithTimes.Count(x => (x.AnsweredAt - x.StartedAt).TotalSeconds <= x.MaxWait);
            slaRate = Math.Round((double)slaCompliant / answeredWithTimes.Count * 100, 1);
        }

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
            AbandonmentRate = abandonmentRate,
            AvgSpeedOfAnswerSeconds = asa,
            AvgHandleTimeSeconds = aht,
            SlaComplianceRate = slaRate,
            Items = new PagedResult<CallReportItemDto>
            {
                Items = items,
                TotalCount = totalCalls,
                Page = page,
                PageSize = pageSize
            }
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // TEMSİLCİ RAPORU
    // ═══════════════════════════════════════════════════════════════

    public async Task<AgentReportResponse> GetAgentReportAsync(int? customerId, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var agentsQuery = _userEs.GetAllQueryable().Where(u => u.IsActive && (u.RoleId == UserRoles.Ids.Agent || u.RoleId == UserRoles.Ids.CustomerUser));

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

    // ═══════════════════════════════════════════════════════════════
    // KUYRUK RAPORU
    // ═══════════════════════════════════════════════════════════════

    public async Task<QueueReportResponse> GetQueueReportAsync(int? customerId, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var queuesQuery = _queueEs.GetAllQueryable().Where(q => q.IsActive);

        if (customerId.HasValue && customerId.Value > 0)
            queuesQuery = queuesQuery.Where(q => q.CustomerId == customerId.Value);

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

        var totalQueues = await queuesQuery.CountAsync();

        var queueList = await queuesQuery
            .OrderBy(q => q.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new { q.Id, q.Name, q.MaxWaitTimeSeconds })
            .ToListAsync();

        var queueIds = queueList.Select(q => q.Id).ToList();

        // Kuyruk bazli cagri verilerini tek sorguda cek
        var queueCallData = await callsQuery
            .Where(c => c.QueueId.HasValue && queueIds.Contains(c.QueueId.Value))
            .Select(c => new { c.QueueId, c.StatusId, c.StartedAt, c.AnsweredAt, c.EndedAt })
            .ToListAsync();

        var items = queueList.Select(q =>
        {
            var calls = queueCallData.Where(c => c.QueueId == q.Id).ToList();
            var total = calls.Count;
            var answered = calls.Count(c => c.StatusId == CallStatuses.Ids.Completed);
            var missed = calls.Count(c => c.StatusId == CallStatuses.Ids.Missed);
            var answeredWithAnswer = calls.Where(c => c.StatusId == CallStatuses.Ids.Completed && c.AnsweredAt.HasValue).ToList();
            var avgWait = answeredWithAnswer.Count > 0 ? (int)answeredWithAnswer.Average(c => (c.AnsweredAt!.Value - c.StartedAt).TotalSeconds) : 0;
            var avgHandle = answeredWithAnswer.Where(c => c.EndedAt.HasValue).ToList() is { Count: > 0 } handled
                ? (int)handled.Average(c => (c.EndedAt!.Value - c.AnsweredAt!.Value).TotalSeconds) : 0;
            var slaCompliant = answeredWithAnswer.Count(c => (c.AnsweredAt!.Value - c.StartedAt).TotalSeconds <= q.MaxWaitTimeSeconds);

            return new QueueReportItemDto
            {
                QueueId = q.Id,
                QueueName = q.Name,
                TotalCalls = total,
                AnsweredCalls = answered,
                MissedCalls = missed,
                AvgWaitSeconds = avgWait,
                AvgHandleSeconds = avgHandle,
                SlaTargetSeconds = q.MaxWaitTimeSeconds,
                SlaComplianceRate = answered > 0 ? Math.Round((double)slaCompliant / answered * 100, 1) : 0,
                AbandonmentRate = total > 0 ? Math.Round((double)missed / total * 100, 1) : 0
            };
        }).ToList();

        var overallSla = items.Count > 0 && items.Any(i => i.AnsweredCalls > 0)
            ? Math.Round(items.Where(i => i.AnsweredCalls > 0).Average(i => i.SlaComplianceRate), 1)
            : 0;

        return new QueueReportResponse
        {
            TotalQueues = totalQueues,
            OverallSlaRate = overallSla,
            Items = new PagedResult<QueueReportItemDto>
            {
                Items = items,
                TotalCount = totalQueues,
                Page = page,
                PageSize = pageSize
            }
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // GUNLUK TREND
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<DailyCallStatsDto>> GetDailyTrendAsync(int? customerId, DateTime? from, DateTime? to)
    {
        var query = await BuildFilteredQueryAsync(customerId, from, to);

        var dailyStats = await query
            .GroupBy(c => c.StartedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalCalls = g.Count(),
                AnsweredCalls = g.Count(c => c.StatusId == CallStatuses.Ids.Completed),
                MissedCalls = g.Count(c => c.StatusId == CallStatuses.Ids.Missed)
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        return dailyStats.Select(d => new DailyCallStatsDto
        {
            Date = DateTime.SpecifyKind(d.Date, DateTimeKind.Utc),
            TotalCalls = d.TotalCalls,
            AnsweredCalls = d.AnsweredCalls,
            MissedCalls = d.MissedCalls
        }).ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    // DURUM DAGILIMI
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<StatusDistributionDto>> GetStatusDistributionAsync(int? customerId, DateTime? from, DateTime? to)
    {
        var query = await BuildFilteredQueryAsync(customerId, from, to);

        var distribution = await query
            .GroupBy(c => c.StatusId)
            .Select(g => new { StatusId = g.Key, Count = g.Count() })
            .ToListAsync();

        return distribution.Select(d => new StatusDistributionDto
        {
            StatusName = CallStatuses.GetById(d.StatusId)?.SystemName ?? "Unknown",
            Count = d.Count
        }).ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    // CSV EXPORT
    // ═══════════════════════════════════════════════════════════════

    public async Task<byte[]> ExportCallReportCsvAsync(int? customerId, DateTime? from, DateTime? to, int? directionId, int? statusId)
    {
        var query = await BuildFilteredQueryAsync(customerId, from, to, directionId, statusId);

        var calls = await query
            .OrderByDescending(c => c.StartedAt)
            .Select(c => new
            {
                c.CallerNumber,
                c.CalleeNumber,
                c.DirectionId,
                c.StatusId,
                c.DurationSeconds,
                c.StartedAt,
                AgentName = c.Agent != null ? c.Agent.FullName : "",
                QueueName = c.Queue != null ? c.Queue.Name : ""
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Arayan;Aranan;Yon;Durum;Sure (sn);Temsilci;Kuyruk;Tarih");
        foreach (var c in calls)
        {
            var direction = CallDirections.GetById(c.DirectionId)?.SystemName ?? "";
            var status = CallStatuses.GetById(c.StatusId)?.SystemName ?? "";
            sb.AppendLine($"{c.CallerNumber};{c.CalleeNumber};{direction};{status};{c.DurationSeconds};{c.AgentName};{c.QueueName};{c.StartedAt:dd.MM.yyyy HH:mm}");
        }

        // UTF-8 BOM for Excel Turkce karakter destegi
        var bom = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[bom.Length + content.Length];
        bom.CopyTo(result, 0);
        content.CopyTo(result, bom.Length);
        return result;
    }

    public async Task<byte[]> ExportAgentReportCsvAsync(int? customerId, DateTime? from, DateTime? to)
    {
        var report = await GetAgentReportAsync(customerId, from, to, 1, 10000);

        var sb = new StringBuilder();
        sb.AppendLine("Temsilci;Dahili;Toplam;Cevaplanan;Cevapsiz;Ort. Sure (sn);Cevaplama Orani (%)");
        foreach (var a in report.Items.Items)
        {
            sb.AppendLine($"{a.FullName};{a.Extension ?? ""};{a.TotalCalls};{a.AnsweredCalls};{a.MissedCalls};{a.AvgDurationSeconds};{a.AnswerRate:0.#}");
        }

        var bom = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[bom.Length + content.Length];
        bom.CopyTo(result, 0);
        content.CopyTo(result, bom.Length);
        return result;
    }

    // ═══════════════════════════════════════════════════════════════
    // EXCEL EXPORT (ClosedXML)
    // ═══════════════════════════════════════════════════════════════

    public async Task<byte[]> ExportCallReportExcelAsync(int? customerId, DateTime? from, DateTime? to, int? directionId, int? statusId)
    {
        var query = await BuildFilteredQueryAsync(customerId, from, to, directionId, statusId);

        var calls = await query
            .OrderByDescending(c => c.StartedAt)
            .Select(c => new
            {
                c.CallerNumber,
                c.CalleeNumber,
                c.DirectionId,
                c.StatusId,
                c.DurationSeconds,
                c.StartedAt,
                AgentName = c.Agent != null ? c.Agent.FullName : "",
                QueueName = c.Queue != null ? c.Queue.Name : ""
            })
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Arama Raporu");

        // Baslik satiri
        var headers = new[] { "Arayan", "Aranan", "Yon", "Durum", "Sure (sn)", "Temsilci", "Kuyruk", "Tarih" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Veri satirlari
        for (int row = 0; row < calls.Count; row++)
        {
            var c = calls[row];
            ws.Cell(row + 2, 1).Value = c.CallerNumber;
            ws.Cell(row + 2, 2).Value = c.CalleeNumber;
            ws.Cell(row + 2, 3).Value = CallDirections.GetById(c.DirectionId)?.SystemName ?? "";
            ws.Cell(row + 2, 4).Value = CallStatuses.GetById(c.StatusId)?.SystemName ?? "";
            ws.Cell(row + 2, 5).Value = c.DurationSeconds;
            ws.Cell(row + 2, 6).Value = c.AgentName;
            ws.Cell(row + 2, 7).Value = c.QueueName;
            ws.Cell(row + 2, 8).Value = c.StartedAt.ToString("dd.MM.yyyy HH:mm");
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportAgentReportExcelAsync(int? customerId, DateTime? from, DateTime? to)
    {
        var report = await GetAgentReportAsync(customerId, from, to, 1, 10000);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Temsilci Raporu");

        var headers = new[] { "Temsilci", "Dahili", "Toplam", "Cevaplanan", "Cevapsiz", "Ort. Sure (sn)", "Cevaplama Orani (%)" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            cell.Style.Font.FontColor = XLColor.White;
        }

        for (int row = 0; row < report.Items.Items.Count; row++)
        {
            var a = report.Items.Items[row];
            ws.Cell(row + 2, 1).Value = a.FullName;
            ws.Cell(row + 2, 2).Value = a.Extension ?? "";
            ws.Cell(row + 2, 3).Value = a.TotalCalls;
            ws.Cell(row + 2, 4).Value = a.AnsweredCalls;
            ws.Cell(row + 2, 5).Value = a.MissedCalls;
            ws.Cell(row + 2, 6).Value = a.AvgDurationSeconds;
            ws.Cell(row + 2, 7).Value = a.AnswerRate;
            ws.Cell(row + 2, 7).Style.NumberFormat.Format = "0.0";
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
