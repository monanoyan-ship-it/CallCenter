using CallCenter.Shared.Enums;

namespace CallCenter.Shared.Entities;

public class CallRecord
{
    public Guid Id { get; set; }
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public CallDirection Direction { get; set; }
    public CallStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationSeconds { get; set; }
    public string? Notes { get; set; }
    public string? RecordingUrl { get; set; }

    public Guid? AgentId { get; set; }
    public User? Agent { get; set; }

    public Guid? QueueId { get; set; }
    public Queue? Queue { get; set; }
}
