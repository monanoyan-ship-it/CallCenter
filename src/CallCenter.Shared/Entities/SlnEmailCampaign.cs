namespace CallCenter.Shared.Entities;

/// <summary>
/// E-posta kampanyasi
/// </summary>
public class SlnEmailCampaign
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? SegmentFilter { get; set; }

    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int OpenCount { get; set; }
    public int ClickCount { get; set; }

    /// <summary>1=Taslak, 2=Planlanmis, 3=Gonderiliyor, 4=Tamamlandi</summary>
    public int StatusId { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
