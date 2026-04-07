namespace CallCenter.Shared.Entities;

/// <summary>
/// Modul katalog fiyati. Her modulun tek bir liste fiyati tutar.
/// Management panelinden yonetilir. Firma ozel fiyat icin CustomerPortalModule.MonthlyPrice kullanilir.
/// </summary>
public class ModulePricing
{
    public int Id { get; set; }

    /// <summary>Modul ID (PortalModules.Ids.* veya SalonPortalModules.Ids.*)</summary>
    public int ModuleId { get; set; }

    /// <summary>Aylik liste fiyati</summary>
    public decimal MonthlyPrice { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
