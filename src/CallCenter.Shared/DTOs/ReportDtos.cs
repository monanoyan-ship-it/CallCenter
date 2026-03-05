namespace CallCenter.Shared.DTOs;

// ═══════════════════════════════════════════════════════════════
// ARAMA RAPORLARI
// ═══════════════════════════════════════════════════════════════

/// <summary>Arama raporu sonucu — ozet istatistikler + sayfalamali liste</summary>
public class CallReportResponse
{
    // Ozet istatistikler
    public int TotalCalls { get; set; }
    public int AnsweredCalls { get; set; }
    public int MissedCalls { get; set; }
    public int AvgDurationSeconds { get; set; }

    // SLA metrikleri
    public double AbandonmentRate { get; set; }
    public int AvgSpeedOfAnswerSeconds { get; set; }
    public int AvgHandleTimeSeconds { get; set; }
    public double SlaComplianceRate { get; set; }

    // Sayfalamali arama listesi
    public PagedResult<CallReportItemDto> Items { get; set; } = new();
}

/// <summary>Rapordaki tek bir arama kaydi</summary>
public class CallReportItemDto
{
    public int Id { get; set; }
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public int DirectionId { get; set; }
    public string DirectionName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public DateTime StartedAt { get; set; }
    public string? AgentName { get; set; }
    public string? QueueName { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// TEMSİLCİ PERFORMANSI
// ═══════════════════════════════════════════════════════════════

/// <summary>Temsilci performans raporu — ozet + sayfalamali liste</summary>
public class AgentReportResponse
{
    // Ozet istatistikler
    public int TotalAgents { get; set; }
    public string? BestPerformerName { get; set; }
    public int LowestAvgAnswerSeconds { get; set; }
    public int OverallAvgDurationSeconds { get; set; }

    // Sayfalamali temsilci listesi
    public PagedResult<AgentReportItemDto> Items { get; set; } = new();
}

/// <summary>Rapordaki tek bir temsilci performansi</summary>
public class AgentReportItemDto
{
    public int AgentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Extension { get; set; }
    public int TotalCalls { get; set; }
    public int AnsweredCalls { get; set; }
    public int MissedCalls { get; set; }
    public int AvgDurationSeconds { get; set; }
    public double AnswerRate { get; set; } // yuzde olarak (0-100)
}

// ═══════════════════════════════════════════════════════════════
// KUYRUK RAPORLARI
// ═══════════════════════════════════════════════════════════════

/// <summary>Kuyruk raporu sonucu</summary>
public class QueueReportResponse
{
    public int TotalQueues { get; set; }
    public double OverallSlaRate { get; set; }
    public PagedResult<QueueReportItemDto> Items { get; set; } = new();
}

/// <summary>Rapordaki tek bir kuyruk performansi</summary>
public class QueueReportItemDto
{
    public int QueueId { get; set; }
    public string QueueName { get; set; } = string.Empty;
    public int TotalCalls { get; set; }
    public int AnsweredCalls { get; set; }
    public int MissedCalls { get; set; }
    public int AvgWaitSeconds { get; set; }
    public int AvgHandleSeconds { get; set; }
    public int SlaTargetSeconds { get; set; }
    public double SlaComplianceRate { get; set; }
    public double AbandonmentRate { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// GRAFIK VERILERI
// ═══════════════════════════════════════════════════════════════

/// <summary>Gunluk cagri istatistigi (trend grafigi)</summary>
public class DailyCallStatsDto
{
    public DateTime Date { get; set; }
    public int TotalCalls { get; set; }
    public int AnsweredCalls { get; set; }
    public int MissedCalls { get; set; }
}

/// <summary>Durum dagilimi (pie chart)</summary>
public class StatusDistributionDto
{
    public string StatusName { get; set; } = string.Empty;
    public int Count { get; set; }
}
