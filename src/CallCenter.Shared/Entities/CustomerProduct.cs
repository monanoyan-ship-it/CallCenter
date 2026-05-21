namespace CallCenter.Shared.Entities;

/// <summary>
/// Musterinin kullandigi urunler (bire-cok). Her urunun ayri fiyatlandirmasi var.
/// ProductTypeId: ProductTypes sabit katalogundan (1=CallCenter, 2=Salon).
/// </summary>
public class CustomerProduct
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>ProductTypes.Ids.CallCenter (1) veya ProductTypes.Ids.Salon (2)</summary>
    public int ProductTypeId { get; set; }

    /// <summary>
    /// Yonetim ekranindaki bilgi/sozlesme fiyati. CallCenter tahakkukunda birim fiyat olarak kullanilmaz.
    /// </summary>
    public decimal MonthlyPrice { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
