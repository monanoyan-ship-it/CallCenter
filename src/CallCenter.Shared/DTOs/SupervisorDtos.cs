namespace CallCenter.Shared.DTOs;

/// <summary>Supervisor dashboard icin tek seferde tum veri</summary>
public class DashboardResponse
{
    // KPI kartlari
    public int ActiveCallCount { get; set; }
    public int AvailableAgentCount { get; set; }
    public int QueueWaitingCount { get; set; }
    public int TodayTotalCallCount { get; set; }
    public int TodayAnsweredCount { get; set; }
    public int TodayMissedCount { get; set; }
    public int TodayAvgDurationSeconds { get; set; }

    // Detay listeleri
    public List<AgentStatusDto> Agents { get; set; } = new();
    public List<RecentCallDto> RecentCalls { get; set; } = new();
    public List<QueueSummaryDto> QueueSummaries { get; set; } = new();
}

/// <summary>Agent durum bilgisi (dashboard'daki temsilci listesi icin)</summary>
public class AgentStatusDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Extension { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusCss { get; set; } = string.Empty;
    public string StatusIcon { get; set; } = string.Empty;
    public int? CurrentCallId { get; set; }
}

/// <summary>Son arama bilgisi (dashboard'daki son aramalar tablosu icin)</summary>
public class RecentCallDto
{
    public int Id { get; set; }
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public int DirectionId { get; set; }
    public string DirectionName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public int DurationSeconds { get; set; }
    public int? AgentId { get; set; }
    public string? AgentName { get; set; }
    public string? QueueName { get; set; }
}

/// <summary>Kuyruk ozet bilgisi</summary>
public class QueueSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int WaitingCount { get; set; }
    public int ActiveCount { get; set; }
    public int AgentCount { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Firma dropdown icin basit musteri bilgisi</summary>
public class CustomerSimpleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
