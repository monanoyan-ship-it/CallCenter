namespace CallCenter.Shared.Entities;

public class DataInventoryItem
{
    public int Id { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string DataCategory { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string LegalBasis { get; set; } = string.Empty;
    public string DataSubjectGroup { get; set; } = string.Empty;
    public string? RecipientGroup { get; set; }
    public string? TransferCountry { get; set; }
    public int RetentionDays { get; set; }
    public string SecurityMeasures { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? VerbisRegistrationNo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
