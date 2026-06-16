namespace CallCenter.Shared.Entities;

/// <summary>
/// Eslesmemis (orphan) ses kaydi.
/// WinApp'te dosya adindan callUid cozulemeyen (eski timestamp-only format) kayitlar
/// buluta yuklenir ve burada tutulur; yetkili kullanici dinleyip dogru CallRecord'a elle esler.
/// Esleme yapilinca hedef CallRecord'a bulut referanslari kopyalanir ve orphan kapatilir.
/// </summary>
public class OrphanRecording
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Orijinal lokal dosya adi (call_yyyyMMdd_HHmmss...). Timestamp/numara tahmini icin.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Dosya adindan/olusturma zamanindan cozulen kayit zamani (tahmini).</summary>
    public DateTime? RecordedAt { get; set; }

    public long FileSize { get; set; }
    public string? FileHash { get; set; }
    public bool IsEncrypted { get; set; }

    // ─── Bulut konum ───

    /// <summary>Musteri deposundaki dosya ID/key (stream icin gerekli).</summary>
    public string? CloudFileId { get; set; }

    /// <summary>Bulut'taki dosya adi (content-type tespiti).</summary>
    public string? CloudFileName { get; set; }

    /// <summary>Platform deposundaki dosya ID/key (varsa).</summary>
    public string? PlatformFileId { get; set; }

    /// <summary>Kaydi yukleyen agent (biliniyorsa).</summary>
    public string? AgentName { get; set; }

    /// <summary>Kaydi yukleyen PC/hostname (biliniyorsa).</summary>
    public string? MachineId { get; set; }

    /// <summary>Saklama son tarihi (TTK md. 82: 10 yil).</summary>
    public DateTime? RetentionDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ─── Eslestirme ───

    public bool IsResolved { get; set; }
    public int? ResolvedCallRecordId { get; set; }
    public CallRecord? ResolvedCallRecord { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedByUserId { get; set; }
}
