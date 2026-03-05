namespace CallCenter.Shared.Entities;

public class DataDestructionLog
{
    public long Id { get; set; } // bigint, yuksek hacim

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int MethodId { get; set; }
    public string DataCategory { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DestroyedRecordCount { get; set; }
    public string? LegalBasis { get; set; }

    public string ApprovedByUserName { get; set; } = string.Empty;
    public int? ApprovedByUserId { get; set; }

    public DateTime DestroyedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
