using CallCenter.Shared.Enums;

namespace CallCenter.Shared.Entities;

public class CallRecord
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public int DirectionId { get; set; } = CallDirections.Ids.Inbound;
    public int StatusId { get; set; } = CallStatuses.Ids.Ringing;
    public DateTime StartedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationSeconds { get; set; }
    public string? Notes { get; set; }
    public string? RecordingUrl { get; set; }

    /// <summary>Musterinin lokal makinesindeki ses kaydi dosya yolu (ses gonderilmez, path bilgisi)</summary>
    public string? RecordingFilePath { get; set; }

    /// <summary>Kayit dosyasinin SHA-256 hash'i (butunluk dogrulamasi)</summary>
    public string? RecordingFileHash { get; set; }

    /// <summary>Kayit dosya boyutu (byte)</summary>
    public long? RecordingFileSize { get; set; }

    /// <summary>Kayit dosyasi AES-256 ile sifrelenmis mi?</summary>
    public bool IsRecordingEncrypted { get; set; }

    /// <summary>Kayit saklama son tarihi (TTK md. 82: 10 yil)</summary>
    public DateTime? RecordingRetentionDate { get; set; }

    // ─── Bulut Depolama ───

    /// <summary>Bulut'taki dosya ID/key (S3 key, Drive file ID, vb.)</summary>
    public string? CloudFileId { get; set; }

    /// <summary>Bulut'taki dosya adi</summary>
    public string? CloudFileName { get; set; }

    /// <summary>Hangi storage config ile yuklendi</summary>
    public int? CloudStorageConfigId { get; set; }

    /// <summary>Bulut'a yuklenme zamani</summary>
    public DateTime? CloudUploadedAt { get; set; }

    // ─── Platform Depolama ───

    /// <summary>Platform deposundaki dosya ID/key</summary>
    public string? PlatformFileId { get; set; }

    /// <summary>Platform deposuna yuklenme zamani</summary>
    public DateTime? PlatformUploadedAt { get; set; }

    // ─── KVKK Rıza Takibi ───

    /// <summary>Arama icin olusturulan riza kaydi</summary>
    public int? ConsentRecordId { get; set; }
    public ConsentRecord? ConsentRecord { get; set; }

    /// <summary>Arama sirasindaki riza durumu (ConsentStatuses)</summary>
    public int? ConsentStatusId { get; set; }

    /// <summary>Aydinlatma metni okundu/dinletildi mi?</summary>
    public bool IsPrivacyNoticeDelivered { get; set; }

    /// <summary>Hangi aydinlatma metni versiyonu okundu?</summary>
    public int? PrivacyNoticeId { get; set; }
    public PrivacyNotice? PrivacyNotice { get; set; }

    public int? AgentId { get; set; }
    public User? Agent { get; set; }

    public int? QueueId { get; set; }
    public Queue? Queue { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    // ─── Geri Arama (Callback) Yonetimi ───

    /// <summary>Geri arama gorev durumu (CallbackStatuses)</summary>
    public int? CallbackStatusId { get; set; }

    /// <summary>Gorevin atandigi temsilci</summary>
    public int? CallbackAssignedToId { get; set; }
    public User? CallbackAssignedTo { get; set; }

    /// <summary>Gorevi atayanin notu</summary>
    public string? CallbackNote { get; set; }

    /// <summary>Geri arama sonucunda olusan yeni cagri kaydi</summary>
    public int? CallbackResultCallId { get; set; }
    public CallRecord? CallbackResultCall { get; set; }
}
