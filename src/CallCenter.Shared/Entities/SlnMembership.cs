namespace CallCenter.Shared.Entities;

/// <summary>
/// Uyelik plani tanimi.
/// DurationType ile sureli veya kullanimlik olabilir.
/// </summary>
public class SlnMembershipPlan
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public string? Color { get; set; }

    /// <summary>1=Sureli (gun bazli), 2=Kullanimlik (hak bazli)</summary>
    public int DurationType { get; set; } = 1;

    /// <summary>Sureli ise: kac gun gecerli (30/90/180/365). Kullanimlik ise: 0</summary>
    public int DurationDays { get; set; } = 30;

    /// <summary>Plan ucreti</summary>
    public decimal Price { get; set; }

    /// <summary>Genel yuzde indirim orani (0-100). Hizmet bazli indirim planService'te</summary>
    public int DiscountPercent { get; set; }

    /// <summary>Oncelikli randevu hakki</summary>
    public bool PriorityBooking { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Plana dahil hizmetler</summary>
    public ICollection<SlnMembershipPlanService> Services { get; set; } = [];
}

/// <summary>
/// Musteriye atanmis uyelik.
/// Sureli: StartDate + DurationDays = EndDate, donem bazli hak sifirlama.
/// Kullanimlik: EndDate yok, haklar bitince biter.
/// </summary>
public class SlnClientMembership
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int PlanId { get; set; }
    public SlnMembershipPlan? Plan { get; set; }

    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    /// <summary>Uyelik baslangici</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Sureli ise: bitis tarihi. Kullanimlik ise: null (haklar bitince biter)</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Sureli ise: mevcut donemin baslangici (hak sifirlama icin)</summary>
    public DateTime? CurrentPeriodStart { get; set; }

    /// <summary>Sureli ise: mevcut donemin bitisi</summary>
    public DateTime? CurrentPeriodEnd { get; set; }

    /// <summary>Odeme tutari</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>1=Aktif, 2=Dondurulmus, 3=Iptal, 4=Suresi Doldu</summary>
    public int StatusId { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
