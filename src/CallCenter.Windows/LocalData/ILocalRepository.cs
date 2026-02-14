using CallCenter.Windows.LocalData.Entities;

namespace CallCenter.Windows.LocalData;

/// <summary>
/// Lokal veritabani erisim katmani.
/// Her veritabani tipi (PostgreSQL, MSSQL, MongoDB) bu interface'i implemente eder.
/// DB ayarlanmamissa NullLocalRepository (no-op) kullanilir.
///
/// Kullanim:
///   var repo = LocalRepositoryFactory.Create("PostgreSQL", connectionString);
///   await repo.InitializeAsync();  // tablolari olustur
///   await repo.SaveCallRecordAsync(record);  // cagri kaydet
/// </summary>
public interface ILocalRepository
{
    // ═══════════════════════════════════════
    // BAGLANTI
    // ═══════════════════════════════════════

    /// <summary>Veritabanina baglanabilir mi? (ayarlar ekranindaki "Test Et" butonu icin)</summary>
    Task<bool> TestConnectionAsync();

    /// <summary>Tablolari/koleksiyonlari olustur (ilk kullanim veya guncelleme)</summary>
    Task InitializeAsync();

    /// <summary>Aktif bir DB baglantisi var mi? (NullLocalRepository icin false)</summary>
    bool IsConfigured { get; }

    // ═══════════════════════════════════════
    // CAGRI KAYITLARI
    // ═══════════════════════════════════════

    /// <summary>Yeni cagri kaydi olustur</summary>
    Task SaveCallRecordAsync(LocalCallRecord record);

    /// <summary>Mevcut cagri kaydini guncelle (ornegin arama bitti, sure hesaplandi)</summary>
    Task UpdateCallRecordAsync(LocalCallRecord record);

    /// <summary>Uid ile tek kayit getir</summary>
    Task<LocalCallRecord?> GetCallRecordByUidAsync(Guid uid);

    /// <summary>Tarih araliginda sayfalanmis cagri kayitlari</summary>
    Task<List<LocalCallRecord>> GetCallRecordsAsync(DateTime? from, DateTime? to, int page, int pageSize);

    /// <summary>Backend'e henuz senkronlanmamis kayitlar (BackgroundSyncService icin)</summary>
    Task<List<LocalCallRecord>> GetUnsyncedRecordsAsync(int limit = 50);

    /// <summary>Kaydı "senkronlandi" olarak isaretle</summary>
    Task MarkAsSyncedAsync(Guid uid, int? backendCallId = null);

    // ═══════════════════════════════════════
    // SES KAYITLARI
    // ═══════════════════════════════════════

    /// <summary>Ses kaydi metadata'sini kaydet</summary>
    Task SaveRecordingMetadataAsync(LocalRecording recording);

    /// <summary>Bir cagriya ait ses kayitlari veya tumu</summary>
    Task<List<LocalRecording>> GetRecordingsAsync(Guid? callRecordUid, int page, int pageSize);

    /// <summary>Saklama suresi dolmus ses kayitlarini getir (RetentionDate &lt; UtcNow)</summary>
    Task<List<LocalRecording>> GetExpiredRecordingsAsync();

    /// <summary>Ses kaydi metadata'sini sil (dosya temizleme sonrasi)</summary>
    Task DeleteRecordingAsync(Guid uid);

    // ═══════════════════════════════════════
    // ISTATISTIKLER (lokal raporlama icin)
    // ═══════════════════════════════════════

    /// <summary>Tarih araliginda istatistik ozeti</summary>
    Task<LocalCallStats> GetStatsAsync(DateTime from, DateTime to);
}
