using CallCenter.Shared.Enums;

namespace CallCenter.Shared.Entities;

/// <summary>
/// KVKK/BTK uyumlu kayit dinleme denetim logu.
/// AuditLog'dan ayri: yuksek hacim, kayit-spesifik, regulasyon gereksinimi.
/// </summary>
public class RecordingAccessLog
{
    public long Id { get; set; }
    public int CallRecordId { get; set; }
    public int? AccessedByUserId { get; set; }
    public string AccessedByUserName { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public int ActionTypeId { get; set; } = RecordingAccessActions.Ids.Play;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? FailureReason { get; set; }
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CallRecord? CallRecord { get; set; }
}
