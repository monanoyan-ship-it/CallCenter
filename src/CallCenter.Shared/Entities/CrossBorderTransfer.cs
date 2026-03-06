namespace CallCenter.Shared.Entities;

public class CrossBorderTransfer
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string RecipientName { get; set; } = string.Empty;
    public string RecipientCountry { get; set; } = string.Empty;
    public string DataCategories { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public int SafeguardId { get; set; }
    public string LegalBasis { get; set; } = string.Empty;

    public DateTime TransferDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
    public string? CreatedByUserName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
