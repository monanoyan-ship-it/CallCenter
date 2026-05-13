namespace CallCenter.Shared.Entities;

public class SlnSupplierOrderItem
{
    public int Id { get; set; }
    public int SupplierOrderId { get; set; }
    public SlnSupplierOrder? SupplierOrder { get; set; }

    public int ProductId { get; set; }
    public SlnProduct? Product { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public string? Notes { get; set; }
}
