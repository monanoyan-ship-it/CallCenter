namespace CallCenter.Shared.Entities;

public class SlnService
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int CategoryId { get; set; }
    public SlnServiceCategory? Category { get; set; }

    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 30;
    public decimal Price { get; set; }
    /// <summary>KDV orani (varsayilan %10)</summary>
    public decimal TaxRate { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
