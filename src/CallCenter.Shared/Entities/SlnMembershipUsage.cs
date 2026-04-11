namespace CallCenter.Shared.Entities;

/// <summary>
/// Uyelik hizmet kullanim takibi.
/// Sureli plan: donem bazli (PeriodStart ile). Her yeni donemde sifirlanir.
/// Kullanimlik plan: toplam bazli (PeriodStart null). Bitince uyelik biter.
/// </summary>
public class SlnMembershipUsage
{
    public int Id { get; set; }
    public int CustomerId { get; set; }

    /// <summary>Hangi musteri uyeligi</summary>
    public int MembershipId { get; set; }
    public SlnClientMembership? Membership { get; set; }

    /// <summary>Hangi hizmet</summary>
    public int ServiceId { get; set; }
    public SlnService? Service { get; set; }

    /// <summary>Sureli: donemin baslangic tarihi. Kullanimlik: null (toplam takip)</summary>
    public DateTime? PeriodStart { get; set; }

    /// <summary>Kullanim sayisi</summary>
    public int UsedCount { get; set; }

    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
}
