namespace CallCenter.Shared.Entities;

public class SlnStockMovement
{
    public int Id { get; set; }
    public int CustomerId { get; set; }

    /// <summary>Hangi sube (null = merkez)</summary>
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public int ProductId { get; set; }
    public SlnProduct? Product { get; set; }

    /// <summary>1=Purchase, 2=Sale, 3=InternalUse, 4=Transfer, 5=Return, 6=StockCountAdjustment</summary>
    public int MovementTypeId { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public int? SupplierId { get; set; }
    public SlnSupplier? Supplier { get; set; }

    public string? Notes { get; set; }
    public int? CreatedByPersonnelId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
