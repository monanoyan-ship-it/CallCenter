namespace CallCenter.Shared.Entities;

public class CustomerServiceSubscription
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    /// <summary>ServiceTypes TypeDefinition ID (FK yok, sabit katalog)</summary>
    public int ServiceTypeId { get; set; }
    public int StatusId { get; set; }
    public decimal MonthlyPrice { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ServiceBillingItem> BillingItems { get; set; } = new List<ServiceBillingItem>();
}
