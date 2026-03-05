namespace CallCenter.Shared.Entities;

public class ConsentRecord
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int ConsentTypeId { get; set; }
    public int StatusId { get; set; }

    public string SubjectIdentifier { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string ConsentMethod { get; set; } = string.Empty; // IVR / Written / Digital
    public string? LegalBasis { get; set; }
    public string? PrivacyNoticeVersion { get; set; }

    public DateTime ConsentDate { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedBy { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
