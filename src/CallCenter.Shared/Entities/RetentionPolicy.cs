namespace CallCenter.Shared.Entities;

public class RetentionPolicy
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int RetentionDays { get; set; }
    public string LegalBasis { get; set; } = string.Empty; // TTK md.82 / KVKK / BTK
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
