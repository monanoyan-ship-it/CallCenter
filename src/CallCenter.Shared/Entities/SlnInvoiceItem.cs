namespace CallCenter.Shared.Entities;

public class SlnInvoiceItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public SlnInvoice? Invoice { get; set; }

    /// <summary>Hizmet satisi (null ise urun)</summary>
    public int? ServiceId { get; set; }
    public SlnService? Service { get; set; }

    /// <summary>Urun satisi (null ise hizmet)</summary>
    public int? ProductId { get; set; }
    public SlnProduct? Product { get; set; }

    /// <summary>Islemi yapan personel</summary>
    public int? PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }

    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>KDV orani (ornegin: 10, 20)</summary>
    public decimal TaxRate { get; set; }

    /// <summary>KDV tutari</summary>
    public decimal TaxAmount { get; set; }
}
