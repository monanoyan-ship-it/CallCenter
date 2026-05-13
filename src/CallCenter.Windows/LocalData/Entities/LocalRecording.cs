namespace CallCenter.Windows.LocalData.Entities;

/// <summary>
/// Ses kaydi metadata'si. Dosyanin kendisi lokal diskte,
/// burada sadece metadata (yol, boyut, sure) tutulur.
/// </summary>
public class LocalRecording
{
    /// <summary>Benzersiz tanimlayici</summary>
    public Guid Uid { get; set; } = Guid.NewGuid();

    /// <summary>Hangi cagriya ait (LocalCallRecord.Uid)</summary>
    public Guid CallRecordUid { get; set; }

    /// <summary>Dosya yolu (lokal disk)</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Dosya boyutu (byte)</summary>
    public long FileSize { get; set; }

    /// <summary>Format: "wav", "mp3"</summary>
    public string Format { get; set; } = "wav";

    /// <summary>Kayit suresi (saniye)</summary>
    public int DurationSeconds { get; set; }

    /// <summary>Kayit olusturulma zamani</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Dosyanin SHA-256 hash'i</summary>
    public string? FileHash { get; set; }

    /// <summary>Dosya AES-256 ile sifrelenmis mi?</summary>
    public bool IsEncrypted { get; set; }

    /// <summary>Saklama son tarihi (TTK md. 82: 10 yil)</summary>
    public DateTime? RetentionDate { get; set; }

    // ─── Musteri Bulut Upload Takibi ───

    /// <summary>Musteri deposuna basariyla yuklendi mi?</summary>
    public bool IsUploadedToCloud { get; set; }

    /// <summary>Son yukleme denemesi zamani</summary>
    public DateTime? LastCloudUploadAttempt { get; set; }

    /// <summary>Toplam yukleme deneme sayisi (5 hizli denemeden sonra aralikli tekrar denenir)</summary>
    public int CloudUploadAttemptCount { get; set; }

    /// <summary>Musteri bulut'undaki dosya ID/key (S3 key, Drive file ID, vb.)</summary>
    public string? CloudFileId { get; set; }

    // ─── Platform Bulut Upload Takibi ───

    /// <summary>Platform deposuna basariyla yuklendi mi?</summary>
    public bool IsUploadedToPlatform { get; set; }

    /// <summary>Platform'daki dosya ID/key</summary>
    public string? PlatformFileId { get; set; }

    /// <summary>Platform upload deneme sayisi</summary>
    public int PlatformUploadAttemptCount { get; set; }

    /// <summary>Son platform upload denemesi zamani</summary>
    public DateTime? LastPlatformUploadAttempt { get; set; }
}
