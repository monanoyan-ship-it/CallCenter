using System.Net.Http.Headers;
using System.Text.Json;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Interfaces;

namespace CallCenter.Api.Services.CloudStorage;

/// <summary>
/// Yandex Disk depolama provider'i.
/// Yandex Disk REST API kullanir (WebDAV yerine daha modern ve guvenilir).
/// OAuth token ile kimlik dogrulama yapar.
/// API: https://cloud-api.yandex.net/v1/disk/resources
/// </summary>
public class YandexDiskStorageProvider : ICloudStorageProvider, IDisposable
{
    private const string ApiBase = "https://cloud-api.yandex.net/v1/disk";
    private readonly HttpClient _httpClient;
    private readonly string? _basePath;

    public int ProviderTypeId => StorageProviders.Ids.YandexDisk;

    public YandexDiskStorageProvider(YandexDiskCredentials credentials)
    {
        _basePath = credentials.BasePath?.TrimEnd('/');

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("OAuth", credentials.OAuthToken);
    }

    public async Task<StorageUploadResult> UploadAsync(Stream fileStream, string fileName,
        string? folder = null, CancellationToken ct = default)
    {
        try
        {
            var diskPath = BuildPath(fileName, folder);

            // 1. Klasor yoksa olustur
            var folderPath = diskPath[..diskPath.LastIndexOf('/')];
            await CreateFolderIfNotExistsAsync(folderPath, ct);

            // 2. Upload URL al
            var encodedPath = Uri.EscapeDataString(diskPath);
            var uploadUrlResponse = await _httpClient.GetAsync(
                $"{ApiBase}/resources/upload?path={encodedPath}&overwrite=true", ct);

            if (!uploadUrlResponse.IsSuccessStatusCode)
            {
                var error = await uploadUrlResponse.Content.ReadAsStringAsync(ct);
                return new StorageUploadResult { Success = false, Error = $"Yandex upload URL hatasi: {error}" };
            }

            var uploadUrlJson = await uploadUrlResponse.Content.ReadAsStringAsync(ct);
            var uploadUrl = JsonDocument.Parse(uploadUrlJson).RootElement.GetProperty("href").GetString();

            if (string.IsNullOrEmpty(uploadUrl))
            {
                return new StorageUploadResult { Success = false, Error = "Yandex upload URL alinamadi" };
            }

            // 3. Dosyayi yukle
            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var uploadResponse = await _httpClient.PutAsync(uploadUrl, content, ct);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                return new StorageUploadResult
                {
                    Success = false,
                    Error = $"Yandex upload hatasi: HTTP {uploadResponse.StatusCode}"
                };
            }

            return new StorageUploadResult
            {
                Success = true,
                FileId = diskPath,
                FileSize = fileStream.Length
            };
        }
        catch (Exception ex)
        {
            return new StorageUploadResult
            {
                Success = false,
                Error = $"Yandex upload hatasi: {ex.Message}"
            };
        }
    }

    public async Task<Stream> DownloadAsync(string fileId, CancellationToken ct = default)
    {
        var encodedPath = Uri.EscapeDataString(fileId);
        var response = await _httpClient.GetAsync(
            $"{ApiBase}/resources/download?path={encodedPath}", ct);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var downloadUrl = JsonDocument.Parse(json).RootElement.GetProperty("href").GetString()
            ?? throw new InvalidOperationException("Yandex download URL alinamadi");

        var downloadResponse = await _httpClient.GetAsync(downloadUrl, ct);
        downloadResponse.EnsureSuccessStatusCode();
        return await downloadResponse.Content.ReadAsStreamAsync(ct);
    }

    public async Task<bool> DeleteAsync(string fileId, CancellationToken ct = default)
    {
        try
        {
            var encodedPath = Uri.EscapeDataString(fileId);
            var response = await _httpClient.DeleteAsync(
                $"{ApiBase}/resources?path={encodedPath}", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string fileId, CancellationToken ct = default)
    {
        try
        {
            var encodedPath = Uri.EscapeDataString(fileId);
            var response = await _httpClient.GetAsync(
                $"{ApiBase}/resources?path={encodedPath}", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetDownloadUrlAsync(string fileId, TimeSpan expiry,
        CancellationToken ct = default)
    {
        try
        {
            var encodedPath = Uri.EscapeDataString(fileId);
            var response = await _httpClient.GetAsync(
                $"{ApiBase}/resources/download?path={encodedPath}", ct);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonDocument.Parse(json).RootElement.GetProperty("href").GetString();
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool Success, string? Error)> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBase}/", ct);

            if (response.IsSuccessStatusCode)
                return (true, null);

            var error = await response.Content.ReadAsStringAsync(ct);
            return (false, $"Yandex Disk baglanti hatasi: HTTP {response.StatusCode} — {error}");
        }
        catch (Exception ex)
        {
            return (false, $"Yandex Disk baglanti hatasi: {ex.Message}");
        }
    }

    /// <summary>Klasor yoksa olusturur (recursive)</summary>
    private async Task CreateFolderIfNotExistsAsync(string folderPath, CancellationToken ct)
    {
        var parts = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var currentPath = "";

        foreach (var part in parts)
        {
            currentPath += "/" + part;
            var encodedPath = Uri.EscapeDataString(currentPath);

            // Var mi kontrol et
            var checkResponse = await _httpClient.GetAsync(
                $"{ApiBase}/resources?path={encodedPath}", ct);

            if (!checkResponse.IsSuccessStatusCode)
            {
                // Olustur
                await _httpClient.PutAsync(
                    $"{ApiBase}/resources?path={encodedPath}", null, ct);
            }
        }
    }

    private string BuildPath(string fileName, string? folder)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(_basePath)) parts.Add(_basePath.TrimStart('/'));
        if (!string.IsNullOrEmpty(folder)) parts.Add(folder.Trim('/'));
        parts.Add(fileName);
        return "/" + string.Join("/", parts);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
