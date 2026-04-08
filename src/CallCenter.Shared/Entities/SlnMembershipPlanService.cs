namespace CallCenter.Shared.Entities;

/// <summary>
/// Uyelik planina dahil olan hizmetler (many-to-many).
/// Plana dahil hizmetler ucretsiz veya indirimli sunulur.
/// </summary>
public class SlnMembershipPlanService
{
    public int Id { get; set; }

    public int PlanId { get; set; }
    public SlnMembershipPlan? Plan { get; set; }

    public int ServiceId { get; set; }
    public SlnService? Service { get; set; }

    /// <summary>Bu hizmete ozel indirim orani (null = planin genel indirimi)</summary>
    public int? DiscountPercent { get; set; }

    /// <summary>Bu hizmet plana dahil ucretsiz mi?</summary>
    public bool IsFree { get; set; }
}
