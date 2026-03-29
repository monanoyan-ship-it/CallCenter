namespace CallCenter.Shared.Entities;

/// <summary>D4 - Personel bazli fiyatlandirma (ayni hizmet, farkli personel = farkli fiyat)</summary>
public class SlnPersonnelServicePrice
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }

    public int ServiceId { get; set; }
    public SlnService? Service { get; set; }

    /// <summary>Bu personelin bu hizmet icin ozel fiyati (null ise standart fiyat)</summary>
    public decimal Price { get; set; }
}

/// <summary>D5 - Hasilat paylasimi / koltuk kiralama modeli</summary>
public class SlnRevenueShare
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }

    /// <summary>1=Yuzde, 2=Sabit Kira, 3=Hibrit (kira + yuzde)</summary>
    public int ModelTypeId { get; set; } = 1;

    /// <summary>Personelin hasilat payi % (orn: 60 = personel %60, salon %40)</summary>
    public decimal PersonnelSharePercent { get; set; } = 60;

    /// <summary>Sabit aylik kira (ModelTypeId=2 veya 3 icin)</summary>
    public decimal MonthlyRent { get; set; }

    /// <summary>Minimum garantili odeme</summary>
    public decimal MinimumGuarantee { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
