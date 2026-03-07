namespace CallCenter.Shared.Entities;

public class CustomerBillingPeriod
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime PeriodStartDate { get; set; }
    public DateTime PeriodEndDate { get; set; }
    public int UserCount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    /// <summary>Hizmet abonelikleri toplam tutari</summary>
    public decimal ServiceAmount { get; set; }
    public int StatusId { get; set; } = 1; // BillingPeriodStatuses
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
