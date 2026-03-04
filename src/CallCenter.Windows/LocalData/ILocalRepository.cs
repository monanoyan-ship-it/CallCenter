using CallCenter.Windows.LocalData.Entities;

namespace CallCenter.Windows.LocalData;

/// <summary>
/// Lokal veri erisim katmani.
/// FileLocalRepository (JSON dosya) veya NullLocalRepository (no-op) kullanilir.
///
/// Kullanim:
///   var repo = LocalRepositoryFactory.Create(basePath);
///   await repo.InitializeAsync();
///   await repo.SaveCallRecordAsync(record);
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

    /// <summary>Cagri kaydini ve iliskili ses kaydi metadata'sini lokalden sil (sync sonrasi temizlik)</summary>
    Task DeleteCallRecordAsync(Guid uid);

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

    /// <summary>Bulut'a henuz yuklenmemis ses kayitlarini getir (max 5 deneme)</summary>
    Task<List<LocalRecording>> GetUnuploadedRecordingsAsync(int limit = 10);

    /// <summary>Ses kaydini "bulut'a yuklendi" olarak isaretle</summary>
    Task MarkRecordingAsUploadedAsync(Guid uid, string? cloudFileId = null);

    /// <summary>Yukleme denemesi sayacini ve zamanini guncelle</summary>
    Task UpdateRecordingUploadAttemptAsync(Guid uid);

    // ═══════════════════════════════════════
    // ISTATISTIKLER (lokal raporlama icin)
    // ═══════════════════════════════════════

    /// <summary>Tarih araliginda istatistik ozeti</summary>
    Task<LocalCallStats> GetStatsAsync(DateTime from, DateTime to);

    // ═══════════════════════════════════════
    // SIP HESAPLARI (GATEWAY)
    // ═══════════════════════════════════════

    /// <summary>Yeni SIP hesabi (gateway) ekle</summary>
    Task SaveSipAccountAsync(LocalSipAccount account);

    /// <summary>Mevcut SIP hesabini guncelle</summary>
    Task UpdateSipAccountAsync(LocalSipAccount account);

    /// <summary>Uid ile SIP hesabi getir</summary>
    Task<LocalSipAccount?> GetSipAccountByUidAsync(Guid uid);

    /// <summary>ID ile SIP hesabi getir</summary>
    Task<LocalSipAccount?> GetSipAccountByIdAsync(int id);

    /// <summary>Tum SIP hesaplarini getir (sayfalanmis)</summary>
    Task<List<LocalSipAccount>> GetAllSipAccountsAsync(int page = 1, int pageSize = 50);

    /// <summary>Varsayilan (IsDefault = true) SIP hesabini getir</summary>
    Task<LocalSipAccount?> GetDefaultSipAccountAsync();

    /// <summary>SIP hesabini sil</summary>
    Task DeleteSipAccountAsync(int id);

    /// <summary>Backend'e henuz senkronlanmamis SIP hesaplari getir</summary>
    Task<List<LocalSipAccount>> GetUnsyncedSipAccountsAsync(int limit = 50);

    /// <summary>SIP hesabini "senkronlandi" olarak isaretle</summary>
    Task MarkSipAccountAsSyncedAsync(Guid uid, int? backendSipAccountId = null);
}
