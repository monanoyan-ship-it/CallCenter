namespace CallCenter.Shared.Entities;

/// <summary>
/// Salon tahakkuk doneminde faturalanan paket/modul kalemi (tarihsel snapshot).
/// Paket satirinda PackageGroupId dolu; eski kayitlarda yalnizca ModuleId olabilir.
/// </summary>
public class CustomerBillingPeriodModuleLine
{
    public int Id { get; set; }

    public int CustomerBillingPeriodId { get; set; }
    public CustomerBillingPeriod CustomerBillingPeriod { get; set; } = null!;

    /// <summary>SalonModuleGroups paket id (aktif fiyat doneminden aylik tutar).</summary>
    public int? PackageGroupId { get; set; }

    /// <summary>Kaynak CustomerPortalModules satiri — paket satirinda genelde bos.</summary>
    public int? CustomerPortalModuleId { get; set; }
    public CustomerPortalModule? CustomerPortalModule { get; set; }

    /// <summary>Temsil modul id — paket satirinda bos olabilir.</summary>
    public int? ModuleId { get; set; }

    /// <summary>Tahakkuk anindaki gorunen ad (paket veya modul).</summary>
    public string ModuleDisplayName { get; set; } = string.Empty;

    /// <summary>Aylik etkin birim fiyat (override veya katalog).</summary>
    public decimal MonthlyUnitPrice { get; set; }

    /// <summary>Donem tutari: MonthlyUnitPrice x plan IntervalMonths (yuvarlanmis).</summary>
    public decimal LineAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
