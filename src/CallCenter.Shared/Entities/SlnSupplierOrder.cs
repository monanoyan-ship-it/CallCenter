namespace CallCenter.Shared.Entities;

public class SlnSupplierOrder
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int SupplierId { get; set; }
    public SlnSupplier? Supplier { get; set; }

    public string OrderNo { get; set; } = string.Empty;
    public int StatusId { get; set; } = 1;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDate { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Notes { get; set; }

    public int? CreatedByPersonnelId { get; set; }
    public CustomerPersonnel? CreatedByPersonnel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<SlnSupplierOrderItem> Items { get; set; } = [];
}
