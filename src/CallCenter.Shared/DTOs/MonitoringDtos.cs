using System.ComponentModel.DataAnnotations;

namespace CallCenter.Shared.DTOs;

public class MonitoringSessionDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int CallRecordId { get; set; }
    public string? CallerNumber { get; set; }
    public string? CalleeNumber { get; set; }
    public int? AgentId { get; set; }
    public string? AgentName { get; set; }
    public int SupervisorId { get; set; }
    public string? SupervisorName { get; set; }
    public int ModeId { get; set; }
    public string ModeName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Notes { get; set; }
}

public class StartMonitoringRequest
{
    [Required]
    public int CallRecordId { get; set; }

    [Range(1, 3)]
    public int ModeId { get; set; } = 1; // Silent
}

public class MonitorableCallDto
{
    public int CallRecordId { get; set; }
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public int? AgentId { get; set; }
    public string? AgentName { get; set; }
    public string? AgentExtension { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? QueueName { get; set; }
    public DateTime StartedAt { get; set; }
    public int DurationSeconds { get; set; }
    public bool IsBeingMonitored { get; set; }
}
