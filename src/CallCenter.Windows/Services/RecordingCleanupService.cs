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
            foreach (var recording in expiredRecordings)
            {
                try
                {
                    if (File.Exists(recording.FilePath))
                    {
                        File.Delete(recording.FilePath);
                        deleted++;
                    }

                    await _localRepo.DeleteRecordingAsync(recording.Uid);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Kayit dosyasi silinemedi: {Path}", recording.FilePath);
                }
            }

            _logger.LogInformation("Kayit temizleme tamamlandi: {Deleted}/{Total} dosya silindi.",
                deleted, expiredRecordings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kayit temizleme hatasi");
        }
    }
}
