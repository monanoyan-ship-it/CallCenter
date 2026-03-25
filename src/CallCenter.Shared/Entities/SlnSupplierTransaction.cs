namespace CallCenter.Shared.Entities;

public class SlnSupplierTransaction
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public SlnSupplier? Supplier { get; set; }

    /// <summary>1=Purchase (borc), 2=Payment (odeme)</summary>
    public int TransactionTypeId { get; set; }

    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
