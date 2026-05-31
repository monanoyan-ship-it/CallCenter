namespace CallCenter.Shared.Entities;

// Sadakat Programi (Punch Card / "10 ziyaret 1 bedava") modeli.
// Earn-as-you-go: musteri tam fiyat oder, sistem ziyareti sayar; N. ziyaret sonrasi 1 bedava odul kazandirir.
// Sadakat Puani (SlnLoyaltyConfig — TL bazli) ile karistirilmaz; ayri sistemdir, paralel calisir.
// Sadakat Paketi (SlnLoyaltyPackage — prepaid bundle) ile de karistirilmaz; bu post-tracking, paket pre-paid.

public class SlnLoyaltyProgram
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Sayilan hizmet — bu hizmetin satisi her olduğunda ilerleme +1.</summary>
    public int ServiceId { get; set; }
    public SlnService? Service { get; set; }

    /// <summary>Kazanilan odul hizmeti — genellikle ayni ServiceId ama farkli olabilir (orn. 10 manikur sonrasi 1 cilt bakimi).</summary>
    public int RewardServiceId { get; set; }
    public SlnService? RewardService { get; set; }

    /// <summary>Odul icin gereken ziyaret sayisi (orn. 10).</summary>
    public int RequiredVisits { get; set; }

    /// <summary>Kazanilan odulun gecerlilik suresi (gun). Null = sinirsiz.</summary>
    public int? RewardValidDays { get; set; }

    /// <summary>Bir musterinin bu programdan kazanabilecegi maks odul sayisi (sinirsiz icin null).</summary>
    public int? MaxRewardsPerClient { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SlnClientLoyaltyProgress
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int ProgramId { get; set; }
    public SlnLoyaltyProgram? Program { get; set; }

    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    /// <summary>Toplam sayilan ziyaret (mevcut tur icin); modulo RequiredVisits sifirlanmaz, kumulatiftir.</summary>
    public int VisitCount { get; set; }
    /// <summary>Bugune kadar kazandirilan toplam odul sayisi.</summary>
    public int RewardsEarned { get; set; }
    /// <summary>Bugune kadar kullanilan odul sayisi (RewardsEarned - RewardsUsed = aktif kullanilabilir odul).</summary>
    public int RewardsUsed { get; set; }

    public DateTime? LastVisitAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SlnLoyaltyProgramReward> Rewards { get; set; } = [];
}

public class SlnLoyaltyProgramReward
{
    public int Id { get; set; }
    public int ProgressId { get; set; }
    public SlnClientLoyaltyProgress? Progress { get; set; }

    public int RewardServiceId { get; set; }
    public SlnService? RewardService { get; set; }

    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Odulu kazandiran invoice item (RequiredVisits'inci ziyaret).</summary>
    public int? EarnedFromInvoiceItemId { get; set; }

    public DateTime? UsedAt { get; set; }
    /// <summary>Odul kullanildigi invoice item (LineTotal=0 kalemi).</summary>
    public int? UsedInvoiceItemId { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
