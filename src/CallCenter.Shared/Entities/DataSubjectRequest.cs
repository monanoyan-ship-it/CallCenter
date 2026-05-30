namespace CallCenter.Shared.Entities;

public class DataSubjectRequest
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int RequestTypeId { get; set; }
    public int StatusId { get; set; }

    public string RequesterName { get; set; } = string.Empty;
    public string RequesterIdentifier { get; set; } = string.Empty; // TC / tel / email
    public string RequesterContact { get; set; } = string.Empty;
    public string RequestDescription { get; set; } = string.Empty;
    public string? ResponseDescription { get; set; }

    public string? AssignedToUserName { get; set; }
    public int? AssignedToUserId { get; set; }

    public DateTime RequestDate { get; set; }
    public DateTime Deadline { get; set; } // +30 gun otomatik
    public DateTime? CompletedAt { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
