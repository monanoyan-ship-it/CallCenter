namespace CallCenter.Shared.Entities;

public class SlnProduct
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public int CategoryId { get; set; }
    public SlnProductCategory? Category { get; set; }

    public int? BrandId { get; set; }
    public SlnProductBrand? Brand { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    /// <summary>KDV orani (varsayilan %20)</summary>
    public decimal TaxRate { get; set; } = 20;
    public decimal StockQuantity { get; set; }
    public decimal MinStockLevel { get; set; }
    public string Unit { get; set; } = "Adet";
    public bool IsActive { get; set; } = true;
}
