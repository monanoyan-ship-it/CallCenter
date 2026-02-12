using CallCenter.Shared.Enums;

namespace CallCenter.Shared.Entities;

public class CallRecord
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public int DirectionId { get; set; } = CallDirections.Ids.Inbound;
    public int StatusId { get; set; } = CallStatuses.Ids.Ringing;
    public DateTime StartedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationSeconds { get; set; }
    public string? Notes { get; set; }
    public string? RecordingUrl { get; set; }

    /// <summary>Musterinin lokal makinesindeki ses kaydi dosya yolu (ses gonderilmez, path bilgisi)</summary>
    public string? RecordingFilePath { get; set; }

    public int? AgentId { get; set; }
    public User? Agent { get; set; }

    public int? QueueId { get; set; }
    public Queue? Queue { get; set; }
}
