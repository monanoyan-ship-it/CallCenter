namespace CallCenter.Shared.Entities;

/// <summary>D2 - Once/sonra fotograf karsilastirma</summary>
public class SlnBeforeAfterPhoto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public int? ServiceId { get; set; }
    public SlnService? Service { get; set; }

    public string? BeforePhotoUrl { get; set; }
    public string? AfterPhotoUrl { get; set; }
    public string? Notes { get; set; }

    public int? PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }

    /// <summary>Profil sayfasinda gosterilsin mi?</summary>
    public bool IsPublic { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
