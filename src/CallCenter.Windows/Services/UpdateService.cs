using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using CallCenter.Shared.DTOs;

namespace CallCenter.Windows.Services;

/// <summary>
/// Windows App guncelleme servisi.
/// Baslatildiginda API'den versiyon kontrol eder.
/// Guncelleme varsa event tetikler, UI bildirim gosterir.
/// Kullanici onaylayinca dosyayi indirir ve installer'i calistirir.
/// </summary>
public class UpdateService
{
    private readonly HttpClient _httpClient;
    private const string AppName = "WindowsApp";

    public event Action<AppVersionInfoDto>? UpdateAvailable;
    public event Action<int>? DownloadProgressChanged;
    public event Action<string>? UpdateError;
    public event Action? DownloadCompleted;

    public bool IsChecking { get; private set; }
    public bool IsDownloading { get; private set; }
    public AppVersionInfoDto? AvailableUpdate { get; private set; }

    public UpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string CurrentVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        }
    }

    public async Task CheckForUpdateAsync()
    {
        if (IsChecking) return;
        IsChecking = true;

        try
        {
            var url = $"api/version/check?appName={AppName}&currentVersion={CurrentVersion}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode) return;

            var info = await response.Content.ReadFromJsonAsync<AppVersionInfoDto>();
            if (info == null) return;

            if (IsNewerVersion(info.LatestVersion, CurrentVersion))
            {
                AvailableUpdate = info;
                UpdateAvailable?.Invoke(info);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Guncelleme kontrolu hatasi: {ex.Message}");
        }
        finally
        {
            IsChecking = false;
        }
    }

    public async Task<string?> DownloadUpdateAsync()
    {
        if (IsDownloading || AvailableUpdate == null || string.IsNullOrEmpty(AvailableUpdate.DownloadUrl))
            return null;

        IsDownloading = true;

        try
        {
            var updateDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CorpLynk", "Updates");
            Directory.CreateDirectory(updateDir);

            var fileName = $"CorpLynk-{AvailableUpdate.LatestVersion}-setup.exe";
            var filePath = Path.Combine(updateDir, fileName);

            // Daha once indirilmis ve tamamsa tekrar indirme
            if (File.Exists(filePath))
            {
                DownloadCompleted?.Invoke();
                return filePath;
            }

            using var response = await _httpClient.GetAsync(AvailableUpdate.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            var tempPath = filePath + ".tmp";

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920);

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;
            var lastProgress = -1;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    var progress = (int)(totalRead * 100 / totalBytes);
                    if (progress != lastProgress)
                    {
                        lastProgress = progress;
                        DownloadProgressChanged?.Invoke(progress);
                    }
                }
            }

            // Temp dosyayi tamamlanan dosyaya tasi
            fileStream.Close();
            File.Move(tempPath, filePath, overwrite: true);

            DownloadCompleted?.Invoke();
            return filePath;
        }
        catch (Exception ex)
        {
            UpdateError?.Invoke($"Indirme hatasi: {ex.Message}");
            return null;
        }
        finally
        {
            IsDownloading = false;
        }
    }

    public bool LaunchInstaller(string installerPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            UpdateError?.Invoke($"Kurulum baslatilamadi: {ex.Message}");
            return false;
        }
    }

    private static bool IsNewerVersion(string latest, string current)
    {
        if (Version.TryParse(latest, out var latestVer) && Version.TryParse(current, out var currentVer))
            return latestVer > currentVer;
        return false;
    }
}
