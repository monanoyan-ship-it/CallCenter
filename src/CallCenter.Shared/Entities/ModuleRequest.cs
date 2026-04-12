namespace CallCenter.Shared.Entities;

/// <summary>
/// Firma tarafindan yapilan modul talebi.
/// Salon sahibi/mudur talep olusturur, admin onaylar/reddeder.
/// Onaylanan talep otomatik olarak CustomerPortalModule'u aktif eder.
/// </summary>
public class ModuleRequest
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    /// <summary>Talep eden firma</summary>
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Talep edilen modul ID (SalonPortalModules.Ids.*)</summary>
    public int ModuleId { get; set; }

    /// <summary>Talep tipi: 1=Aktivasyon, 2=Iptal (ModuleRequestTypes)</summary>
    public int RequestTypeId { get; set; } = 1;

    /// <summary>Talebi olusturan personel</summary>
    public int RequestedByPersonnelId { get; set; }
    public CustomerPersonnel RequestedByPersonnel { get; set; } = null!;

    /// <summary>Talep durumu (ModuleRequestStatuses TypeItem)</summary>
    public int StatusId { get; set; }

    /// <summary>Firma tarafindan eklenen not</summary>
    public string? RequestNotes { get; set; }

    /// <summary>Admin tarafindan eklenen not</summary>
    public string? AdminNotes { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Talebi degerlendiren admin</summary>
    public int? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
}
