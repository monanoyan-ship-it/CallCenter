using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using CallCenter.Shared.DTOs;

namespace CallCenter.PbxService.Services;

/// <summary>
/// PBX Service guncelleme kontrolu.
/// Baslangicta ve her 1 saatte bir API'den versiyon kontrol eder.
/// Guncelleme varsa indirir ve servisi yeniden baslatir.
/// </summary>
public class UpdateCheckService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UpdateCheckService> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private const string AppName = "PbxService";

    public UpdateCheckService(
        IHttpClientFactory httpClientFactory,
        ILogger<UpdateCheckService> logger,
        IHostApplicationLifetime lifetime)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _lifetime = lifetime;
    }

    private string CurrentVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ilk kontrol: servis basladiktan 30 saniye sonra
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndUpdateAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Guncelleme kontrolu basarisiz");
            }

            // Sonraki kontrol: 1 saat sonra
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CheckAndUpdateAsync(CancellationToken ct)
    {
        var httpClient = _httpClientFactory.CreateClient("CallCenterApi");
        var url = $"/api/version/check?appName={AppName}&currentVersion={CurrentVersion}";

        var response = await httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return;

        var info = await response.Content.ReadFromJsonAsync<AppVersionInfoDto>(cancellationToken: ct);
        if (info == null) return;

        if (!IsNewerVersion(info.LatestVersion, CurrentVersion))
        {
            _logger.LogDebug("PBX Service guncel: v{Version}", CurrentVersion);
            return;
        }

        _logger.LogInformation("Yeni PBX Service versiyonu mevcut: v{New} (mevcut: v{Current})",
            info.LatestVersion, CurrentVersion);

        if (string.IsNullOrEmpty(info.DownloadUrl))
        {
            _logger.LogWarning("Guncelleme URL'si tanimli degil, guncelleme atlanacak");
            return;
        }

        // Dosyayi indir
        var updateDir = Path.Combine(AppContext.BaseDirectory, "updates");
        Directory.CreateDirectory(updateDir);

        var fileName = $"PbxService-{info.LatestVersion}.zip";
        var filePath = Path.Combine(updateDir, fileName);

        if (!File.Exists(filePath))
        {
            _logger.LogInformation("Guncelleme indiriliyor: {Url}", info.DownloadUrl);
            using var downloadResponse = await httpClient.GetAsync(info.DownloadUrl, ct);
            downloadResponse.EnsureSuccessStatusCode();

            var tempPath = filePath + ".tmp";
            await using var contentStream = await downloadResponse.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920);
            await contentStream.CopyToAsync(fileStream, ct);
            fileStream.Close();

            File.Move(tempPath, filePath, overwrite: true);
            _logger.LogInformation("Guncelleme indirildi: {Path}", filePath);
        }

        // Guncelleme script'i olustur ve servisi yeniden baslat
        var scriptPath = CreateUpdateScript(filePath, info.LatestVersion);
        _logger.LogInformation("Guncelleme baslatiliyor, servis yeniden baslayacak...");

        Process.Start(new ProcessStartInfo
        {
            FileName = scriptPath,
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        // Servisi durdur (script bekleyip yeni versiyonu baslatacak)
        _lifetime.StopApplication();
    }

    private string CreateUpdateScript(string zipPath, string version)
    {
        var appDir = AppContext.BaseDirectory;
        var backupDir = Path.Combine(appDir, "backup");
        var scriptPath = Path.Combine(Path.GetTempPath(), "pbx-update.bat");

        var script = $"""
            @echo off
            echo PBX Service guncelleniyor v{version}...
            timeout /t 5 /nobreak >nul

            if exist "{backupDir}" rd /s /q "{backupDir}"
            mkdir "{backupDir}"

            echo Mevcut dosyalar yedekleniyor...
            xcopy /s /q /y "{appDir}*.*" "{backupDir}\" >nul 2>&1

            echo Yeni versiyon cikariliyor...
            tar -xf "{zipPath}" -C "{appDir}" 2>nul
            if errorlevel 1 (
                echo Guncelleme basarisiz, yedek geri yukleniyor...
                xcopy /s /q /y "{backupDir}\*.*" "{appDir}" >nul 2>&1
            )

            echo PBX Service yeniden baslatiliyor...
            cd /d "{appDir}"
            start "" "CallCenter.PbxService.exe"

            del /q "{zipPath}" >nul 2>&1
            exit
            """;

        File.WriteAllText(scriptPath, script);
        return scriptPath;
    }

    private static bool IsNewerVersion(string latest, string current)
    {
        if (Version.TryParse(latest, out var latestVer) && Version.TryParse(current, out var currentVer))
            return latestVer > currentVer;
        return false;
    }
}
