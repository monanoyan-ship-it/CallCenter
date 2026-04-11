namespace CallCenter.Shared.Entities;

/// <summary>
/// Uyelik hizmet kullanim takibi.
/// Musteri + Hizmet + Yil + Ay bazli kac kez ucretsiz kullanildigi.
/// Sales'te adisyon olusturulurken guncellenir.
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

    public int Year { get; set; }
    public int Month { get; set; }

    /// <summary>Bu ay kac kez ucretsiz kullanildi</summary>
    public int UsedCount { get; set; }
}
