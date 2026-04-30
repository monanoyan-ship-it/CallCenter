namespace CallCenter.Shared.Entities;

/// <summary>
/// Fiyat dönemine göre toplam aktif şube sayısı aralığı ve ek şube satırına uygulanacak indirim.
/// İlk (tek) şube zaten ücretsiz; indirim <see cref="CustomerBillingPeriod"/> içindeki şube tutarına uygulanır.
/// </summary>
public class ServicePricingBranchDiscountTier
{
    public int Id { get; set; }

    public int PeriodId { get; set; }
    public ServicePricingPeriod? Period { get; set; }

    /// <summary>Toplam aktif şube sayısı dahil alt sınır (örn. 2).</summary>
    public int MinBranches { get; set; }

    /// <summary>Toplam aktif şube sayısı dahil üst sınır (örn. 10).</summary>
    public int MaxBranches { get; set; }

    /// <summary>Ek şube kalemi üzerinde yüzde indirim (0–100).</summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>Gösterim ve çakışma önceliği (küçük önce).</summary>
    public int SortOrder { get; set; }
}
