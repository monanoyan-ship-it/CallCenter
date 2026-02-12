using CallCenter.Windows.LocalData;
using CallCenter.Windows.LocalData.Entities;

namespace CallCenter.Windows.Services;

/// <summary>
/// Lokal veritabanindan raporlama servisi.
///
/// ILocalRepository uzerinden lokal DB'den veri ceker.
/// Web'deki raporlar backend DB'den gelir,
/// bu servis ise Windows uygulamasindaki lokal raporlar icin kullanilir.
///
/// DB yapilandirilmamissa (NullLocalRepository) bos sonuclar dondurur.
/// </summary>
public class LocalReportService
{
    private readonly ILocalRepository _localRepo;

    public LocalReportService(ILocalRepository localRepo)
    {
        _localRepo = localRepo;
    }

    /// <summary>Lokal DB yapilandirilmis mi?</summary>
    public bool IsAvailable => _localRepo.IsConfigured;

    // ═══════════════════════════════════════
    // ISTATISTIKLER
    // ═══════════════════════════════════════

    /// <summary>
    /// Belirli tarih araliginda cagri istatistikleri.
    /// Toplam, cevaplanan, cevapsiz, ortalama sure.
    /// </summary>
    public async Task<LocalCallStats> GetCallStatsAsync(DateTime from, DateTime to)
    {
        if (!_localRepo.IsConfigured)
            return new LocalCallStats();

        return await _localRepo.GetStatsAsync(from, to);
    }

    // ═══════════════════════════════════════
    // CAGRI LISTESI
    // ═══════════════════════════════════════

    /// <summary>
    /// Tarih araliginda sayfalanmis cagri kayitlari.
    /// Lokal DB'den getirir — backend'ten degil.
    /// </summary>
    public async Task<List<LocalCallRecord>> GetCallRecordsAsync(
        DateTime? from, DateTime? to, int page = 1, int pageSize = 25)
    {
        if (!_localRepo.IsConfigured)
            return new List<LocalCallRecord>();

        return await _localRepo.GetCallRecordsAsync(from, to, page, pageSize);
    }

    // ═══════════════════════════════════════
    // SES KAYITLARI
    // ═══════════════════════════════════════

    /// <summary>
    /// Bir cagriya ait ses kayitlari veya tum ses kayitlari.
    /// Dosya yolunu dondurur — dinleme icin Windows dosya sistemi kullanilir.
    /// </summary>
    public async Task<List<LocalRecording>> GetRecordingsAsync(
        Guid? callRecordUid = null, int page = 1, int pageSize = 25)
    {
        if (!_localRepo.IsConfigured)
            return new List<LocalRecording>();

        return await _localRepo.GetRecordingsAsync(callRecordUid, page, pageSize);
    }
}
