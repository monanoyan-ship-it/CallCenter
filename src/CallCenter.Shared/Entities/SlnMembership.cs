namespace CallCenter.Shared.Entities;

/// <summary>
/// Uyelik plani tanimi (orn: Gold Uye - aylik 500 TL, %20 indirim)
/// </summary>
public class SlnMembershipPlan
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public string? Color { get; set; }

    /// <summary>Aylik ucret</summary>
    public decimal MonthlyPrice { get; set; }

    /// <summary>Yuzde indirim orani (0-100)</summary>
    public int DiscountPercent { get; set; }

    /// <summary>Aylik ucretsiz seans hakki (0=sinirsiz degil)</summary>
    public int FreeSessionsPerMonth { get; set; }

    /// <summary>Oncelikli randevu hakki</summary>
    public bool PriorityBooking { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Plana dahil hizmetler</summary>
    public ICollection<SlnMembershipPlanService> Services { get; set; } = [];
}

/// <summary>
/// Musteriye atanmis uyelik
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

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime NextPaymentDate { get; set; }

    /// <summary>Bu ay kullanilan ucretsiz seans</summary>
    public int UsedFreeSessionsThisMonth { get; set; }

    /// <summary>1=Aktif, 2=Dondurulmus, 3=Iptal</summary>
    public int StatusId { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
