namespace CallCenter.Shared.DTOs;

/// <summary>Yeni arama baslatma request'i</summary>
public class StartCallRequest
{
    /// <summary>Windows app lokal Uid gonderirse backend ayni Uid'yi kullanir (duplicate onleme)</summary>
    public Guid? Uid { get; set; }
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public int? QueueId { get; set; }
}

/// <summary>Arama baslat response'u (Id + Uid)</summary>
public class CallStartResult
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
}

/// <summary>Gelen arama request'i (PBX/webhook'tan)</summary>
public class IncomingCallRequest
{
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public int? QueueId { get; set; }
}

/// <summary>
/// Windows uygulamasinin lokal DB'den backend'e senkronladigi cagri verisi.
/// Uid bazli idempotent — ayni Uid tekrar gelirse gunceller, yeni kayit olusturmaz.
/// BackgroundSyncService tarafindan kullanilir.
/// </summary>
public class CallSyncPushRequest
{
    public Guid Uid { get; set; }
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public string Direction { get; set; } = "Outbound";
    public string Status { get; set; } = "Completed";
    public DateTime StartedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationSeconds { get; set; }
    public string? AgentName { get; set; }
    public string? QueueName { get; set; }
    public string? Notes { get; set; }

    /// <summary>Musterinin lokal makinesindeki ses kaydi dosya yolu (ses dosyasi gonderilmez, sadece path bilgisi)</summary>
    public string? RecordingFilePath { get; set; }

    /// <summary>Kayit dosyasinin SHA-256 hash'i</summary>
    public string? RecordingFileHash { get; set; }

    /// <summary>Kayit dosya boyutu (byte)</summary>
    public long? RecordingFileSize { get; set; }

    /// <summary>Kayit AES-256 ile sifrelenmis mi?</summary>
    public bool IsRecordingEncrypted { get; set; }

    /// <summary>Kayit saklama son tarihi (TTK md. 82)</summary>
    public DateTime? RecordingRetentionDate { get; set; }

    // ─── Bulut Depolama (Windows app direkt yukler) ───

    /// <summary>Bulut'taki dosya ID/key (S3 key, Drive file ID, vb.)</summary>
    public string? CloudFileId { get; set; }

    /// <summary>Bulut'taki dosya adi</summary>
    public string? CloudFileName { get; set; }

    /// <summary>Bulut'a yuklenme zamani</summary>
    public DateTime? CloudUploadedAt { get; set; }

    // ─── Platform Depolama ───

    /// <summary>Platform deposundaki dosya ID/key</summary>
    public string? PlatformFileId { get; set; }

    /// <summary>Platform deposuna yuklenme zamani</summary>
    public DateTime? PlatformUploadedAt { get; set; }
}

/// <summary>Backend'in sync push'a verdigi yanit</summary>
public class CallSyncPushResponse
{
    public int Id { get; set; }
    public bool IsNew { get; set; }
}

/// <summary>Cagri notu guncelleme request'i</summary>
public class UpdateCallNotesRequest
{
    public string? Notes { get; set; }
}
