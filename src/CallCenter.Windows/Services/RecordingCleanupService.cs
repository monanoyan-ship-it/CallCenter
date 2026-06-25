using System.IO;
using CallCenter.Windows.LocalData;
using Microsoft.Extensions.Logging;

namespace CallCenter.Windows.Services;

/// <summary>
/// Saklama suresi dolmus ses kaydi dosyalarini temizler.
/// Uygulama basladiginda bir kez calisir.
/// TTK md. 82: 10 yil saklama suresi varsayilan.
/// </summary>
public class RecordingCleanupService
{
    private readonly ILocalRepository _localRepo;
    private readonly ILogger<RecordingCleanupService> _logger;

    public RecordingCleanupService(ILocalRepository localRepo, ILogger<RecordingCleanupService> logger)
    {
        _localRepo = localRepo;
        _logger = logger;
    }

    /// <summary>
    /// Suresi dolmus kayitlari tespit edip dosyalarini siler.
    /// Uygulama basladiginda MainWindow'dan cagirilir.
    /// </summary>
    public async Task CleanupExpiredRecordingsAsync()
    {
        if (!_localRepo.IsConfigured)
        {
            _logger.LogDebug("Lokal DB yapilandirilmamis, kayit temizleme atlanacak.");
            return;
        }

        try
        {
            var expiredRecordings = await _localRepo.GetExpiredRecordingsAsync();

            if (expiredRecordings.Count == 0)
            {
                _logger.LogDebug("Suresi dolmus kayit bulunamadi.");
                return;
            }

            int deleted = 0;
            int keptUnuploaded = 0;
            foreach (var recording in expiredRecordings)
            {
                try
                {
                    var fileExists = File.Exists(recording.FilePath);

                    // GUVENLIK KORUMASI (asla kaybolmasin):
                    // Henuz hicbir kalici depoya (musteri/platform) yuklenmemis bir kaydin
                    // yerel dosyasini, retention suresi dolmus olsa bile ASLA silme.
                    // Aksi halde dosya portala hic ulasmadan kalici olarak kaybolur.
                    // Dosya hala diskteyse upload pipeline'i tekrar deneyecektir; bu yuzden
                    // hem dosyayi hem metadata'yi koru.
                    var uploadedSomewhere = recording.IsUploadedToCloud || recording.IsUploadedToPlatform;
                    if (fileExists && !uploadedSomewhere)
                    {
                        keptUnuploaded++;
                        continue;
                    }

                    if (fileExists)
                    {
                        File.Delete(recording.FilePath);
                        deleted++;
                    }

                    // Dosya yok (zaten yuklenip silinmis) veya yuklenmis: metadata temizlenebilir.
                    await _localRepo.DeleteRecordingAsync(recording.Uid);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Kayit dosyasi silinemedi: {Path}", recording.FilePath);
                }
            }

            if (keptUnuploaded > 0)
                _logger.LogWarning(
                    "Kayit temizleme: {Kept} adet suresi dolmus ama HENUZ YUKLENMEMIS kayit korundu (kaybolmamasi icin silinmedi).",
                    keptUnuploaded);

            _logger.LogInformation("Kayit temizleme tamamlandi: {Deleted}/{Total} dosya silindi, {Kept} korundu.",
                deleted, expiredRecordings.Count, keptUnuploaded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kayit temizleme hatasi");
        }
    }
}
