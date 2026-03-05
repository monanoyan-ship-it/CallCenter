namespace CallCenter.Shared.Entities;

public class ServiceBillingItem
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int CustomerServiceSubscriptionId { get; set; }
    public CustomerServiceSubscription CustomerServiceSubscription { get; set; } = null!;
    public int StatusId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
