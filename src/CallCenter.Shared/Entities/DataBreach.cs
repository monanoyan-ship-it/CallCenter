namespace CallCenter.Shared.Entities;

public class DataBreach
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int SeverityId { get; set; }
    public int StatusId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DateTime DetectedAt { get; set; }
    public DateTime NotificationDeadline { get; set; } // +72 saat otomatik
    public DateTime? AuthorityNotifiedAt { get; set; }
    public DateTime? SubjectsNotifiedAt { get; set; }

    public int? AffectedSubjectCount { get; set; }
    public string? AffectedDataCategories { get; set; }
    public string? CauseSummary { get; set; }
    public string? MeasuresTaken { get; set; }

    public string? ReportedByUserName { get; set; }
    public int? ReportedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
