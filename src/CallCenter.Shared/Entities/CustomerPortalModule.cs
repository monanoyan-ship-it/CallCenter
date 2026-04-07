namespace CallCenter.Shared.Entities;

/// <summary>
/// Musteriye acik olan portal modulleri.
/// Hangi musterinin hangi modullere erisimi oldugunu belirler.
/// Modul tanimlari PortalModules TypeItem'inda, atamalar bu tabloda.
/// </summary>
public class CustomerPortalModule
{
    public int Id { get; set; }

    /// <summary>Hangi musteriye ait</summary>
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Modul ID (PortalModules.Ids.*)</summary>
    public int ModuleId { get; set; }

    /// <summary>Bu modul aktif mi?</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Modul acilis tarihi</summary>
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Modul kapatis tarihi (null = suresiz)</summary>
    public DateTime? DeactivatedAt { get; set; }

    /// <summary>Aciklama / not (ornegin: "3 aylik deneme", "premium paket")</summary>
    public string? Notes { get; set; }

    /// <summary>Firma bazli fiyat override (null = katalog fiyati kullanilir)</summary>
    public decimal? MonthlyPrice { get; set; }

    /// <summary>Deneme suresi bitis tarihi (null = trial yok)</summary>
    public DateTime? TrialEndsAt { get; set; }
}
