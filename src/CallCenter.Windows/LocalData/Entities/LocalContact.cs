namespace CallCenter.Windows.LocalData.Entities;

/// <summary>
/// API yazimi basarisiz oldugunda gecici olarak lokal buffer'a kaydedilen kisi kaydi.
/// BackgroundSyncService tarafindan periyodik olarak backend'e push edilir.
/// </summary>
public class LocalContact
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    /// <summary>Yapilacak islem: Create, Update, Delete</summary>
    public string Operation { get; set; } = "Create";

    /// <summary>Backend'deki contact ID (Update/Delete icin)</summary>
    public int? BackendContactId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Company { get; set; }
    public bool IsFavorite { get; set; }

    public bool IsSyncedToBackend { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncAttempt { get; set; }
}
