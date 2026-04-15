namespace CallCenter.Shared.Entities;

public class SlnWaitlistEntry
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Hangi şubeye ait olduğu (null = atanmamış / merkez)</summary>
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public int ServiceId { get; set; }
    public SlnService? Service { get; set; }

    public int? PreferredPersonnelId { get; set; }
    public CustomerPersonnel? PreferredPersonnel { get; set; }

    public DateTime PreferredDate { get; set; }
    public string? PreferredTimeSlot { get; set; }
    public string? Notes { get; set; }

    /// <summary>1=Bekliyor, 2=Bildirildi, 3=Randevu Alindi, 4=Iptal</summary>
    public int StatusId { get; set; } = 1;

    public DateTime? NotifiedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
