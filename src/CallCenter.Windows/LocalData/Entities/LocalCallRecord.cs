namespace CallCenter.Windows.LocalData.Entities;

/// <summary>
/// Lokal veritabaninda saklanan cagri kaydi.
/// Backend'teki CallRecord'dan bagimsiz — FK iliskisi yok, sadece Uid referansi.
/// IsSyncedToBackend: Backend'e push edildi mi? (cift yazim takibi)
/// </summary>
public class LocalCallRecord
{
    /// <summary>Benzersiz tanimlayici — backend ile eslesme icin</summary>
    public Guid Uid { get; set; } = Guid.NewGuid();

    /// <summary>Arayan numara/dahili</summary>
    public string CallerNumber { get; set; } = string.Empty;

    /// <summary>Aranan numara</summary>
    public string CalleeNumber { get; set; } = string.Empty;

    /// <summary>Arama yonu: "Inbound" veya "Outbound"</summary>
    public string Direction { get; set; } = "Outbound";

    /// <summary>Arama durumu: "Ringing", "InProgress", "Completed", "Missed", "Failed"</summary>
    public string Status { get; set; } = "Ringing";

    /// <summary>Arama baslangic zamani</summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Cevaplanma zamani (null = cevaplanmadi)</summary>
    public DateTime? AnsweredAt { get; set; }

    /// <summary>Bitis zamani (null = devam ediyor)</summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>Toplam gorusme suresi (saniye)</summary>
    public int DurationSeconds { get; set; }

    /// <summary>Gorusmeyi yapan temsilcinin adi</summary>
    public string? AgentName { get; set; }

    /// <summary>Kuyruk adi (varsa)</summary>
    public string? QueueName { get; set; }

    /// <summary>Gorusme notlari</summary>
    public string? Notes { get; set; }

    /// <summary>Ses kaydi dosya yolu (lokal disk)</summary>
    public string? RecordingFilePath { get; set; }

    // ── Senkronizasyon ──

    /// <summary>Backend API'ye basariyla push edildi mi?</summary>
    public bool IsSyncedToBackend { get; set; }

    /// <summary>Son senkronizasyon denemesi zamani</summary>
    public DateTime? LastSyncAttempt { get; set; }

    /// <summary>Backend'teki CallRecord ID'si (push sonrasi doner)</summary>
    public int? BackendCallId { get; set; }
}
