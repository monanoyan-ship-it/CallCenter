namespace CallCenter.Shared.Entities;

/// <summary>
/// Müşteri–plan bağlantısı: faturalama aralığı, sözleşme tabanı (<see cref="PeriodPrice"/>), isteğe bağlı indirim üst yazımı.
/// Sonraki tahakkuk tarihi tutulmaz; son <see cref="CustomerBillingPeriod"/> (SalonPlatform) + plan aralığından türetilir.
/// </summary>
public class CustomerSubscription
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;

    /// <summary>Hangi sube icin — null = tum firma (eski davranis)</summary>
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    /// <summary>Abonelik baslangic tarihi (ilk odeme tarihi)</summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Müşteri–plan bağlantısında anlaşılmış temel paket aylığı (&gt;0 = liste indirimi uygulanmaz, doğrudan × interval).
    /// 0 = aktif dönem listesi + <see cref="DiscountPercentOverride"/> / <see cref="SubscriptionPlan.DiscountPercent"/>.
    /// Tahakkuk özeti için senkronize tutulur.
    /// </summary>
    public decimal MonthlyPrice { get; set; }

    /// <summary>
    /// Sozlesme donem tutari (yonetim abonelik formu). 0 = ucretli sozlesme yok / deneme (salon kayit).
    /// </summary>
    public decimal PeriodPrice { get; set; }

    /// <summary>Plandaki indirim yerine geçer; null ise <see cref="SubscriptionPlan.DiscountPercent"/>.</summary>
    public decimal? DiscountPercentOverride { get; set; }

    /// <summary>Tahakkuk gunu (ayin kaci). StartDate'in gunu.</summary>
    public int BillingDay { get; set; }

    /// <summary>1=Aktif, 2=Askiya Alinmis, 3=Iptal</summary>
    public int StatusId { get; set; } = 1;

    /// <summary>Odeme icin ek gunler — ucretli suspend mantigi PeriodStartDate ile hizali kullanilir; varsayilan 5.</summary>
    public int PaymentGraceDays { get; set; } = 5;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
}
