namespace CallCenter.Shared.Entities;

public class SlnProductBranchStock
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int ProductId { get; set; }
    public SlnProduct? Product { get; set; }

    public int BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public decimal StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
