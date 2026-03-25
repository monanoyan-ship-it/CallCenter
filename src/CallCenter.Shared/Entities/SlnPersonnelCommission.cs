namespace CallCenter.Shared.Entities;

/// <summary>Personel prim tanimlari (hizmet veya urun bazli)</summary>
public class SlnPersonnelCommission
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }

    /// <summary>Hizmet bazli prim (null ise genel)</summary>
    public int? ServiceId { get; set; }
    public SlnService? Service { get; set; }

    /// <summary>Urun bazli prim (null ise genel)</summary>
    public int? ProductId { get; set; }
    public SlnProduct? Product { get; set; }

    /// <summary>true=yuzde, false=sabit tutar</summary>
    public bool IsPercentage { get; set; } = true;
    public decimal Rate { get; set; }
}
