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
}
