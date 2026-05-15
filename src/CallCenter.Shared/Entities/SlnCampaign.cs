namespace CallCenter.Shared.Entities;

public class SlnCampaign
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public string Name { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public string? SegmentFilter { get; set; } // JSON - filtre kriterleri
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }

    /// <summary>1=Draft, 2=Scheduled, 3=Sending, 4=Completed</summary>
    public int StatusId { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
