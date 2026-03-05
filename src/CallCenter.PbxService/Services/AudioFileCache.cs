namespace CallCenter.PbxService.Services;

/// <summary>
/// API'den ses dosyalarini indirip lokal cache'e kaydeder.
/// IVR karsilama, bekleme muzigi, mesai disi mesaji icin kullanilir.
/// </summary>
public class AudioFileCache
{
    private readonly ILogger<AudioFileCache> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _cacheDir;

    public AudioFileCache(IHttpClientFactory httpClientFactory, ILogger<AudioFileCache> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CallCenterApi");
        _logger = logger;
        _cacheDir = Path.Combine(AppContext.BaseDirectory, "audio-cache");
        Directory.CreateDirectory(_cacheDir);
    }

    /// <summary>Ses dosyasini cache'den getir, yoksa API'den indir</summary>
    public async Task<string?> GetAudioFileAsync(int greetingMessageId)
    {
        var localPath = Path.Combine(_cacheDir, $"greeting_{greetingMessageId}.wav");

        if (File.Exists(localPath))
            return localPath;

        try
        {
            var response = await _httpClient.GetAsync($"/api/pbx/audio/{greetingMessageId}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ses dosyasi indirilemedi: {Id} ({Status})",
                    greetingMessageId, response.StatusCode);
                return null;
            }

            await using var fileStream = File.Create(localPath);
            await response.Content.CopyToAsync(fileStream);

            _logger.LogInformation("Ses dosyasi indirildi: {Id} -> {Path}",
                greetingMessageId, localPath);
            return localPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ses dosyasi indirme hatasi: {Id}", greetingMessageId);
            return null;
        }
    }

    /// <summary>Cache'i temizle (config degistiginde)</summary>
    public void ClearCache()
    {
        try
        {
            if (Directory.Exists(_cacheDir))
            {
                foreach (var file in Directory.GetFiles(_cacheDir))
                    File.Delete(file);
                _logger.LogInformation("Ses cache temizlendi");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache temizleme hatasi");
        }
    }
}
