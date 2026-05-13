using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Interfaces;

namespace CallCenter.Api.Services.CloudStorage;

/// <summary>
/// Microsoft OneDrive storage provider. Uses Microsoft Graph REST directly.
/// </summary>
public class OneDriveStorageProvider : ICloudStorageProvider
{
    private readonly OneDriveCredentials _credentials;
    private readonly string? _driveId;
    private readonly string? _folderId;

    public int ProviderTypeId => StorageProviders.Ids.OneDrive;

    public OneDriveStorageProvider(OneDriveCredentials credentials)
    {
        _credentials = credentials;
        _driveId = credentials.DriveId;
        _folderId = credentials.FolderId;
    }

    public async Task<StorageUploadResult> UploadAsync(Stream fileStream, string fileName,
        string? folder = null, CancellationToken ct = default)
    {
        try
        {
            var driveId = EnsureDriveId();
            var accessToken = await GetAccessTokenAsync(ct);
            var targetPath = BuildPath(fileName, folder);

            if (fileStream.CanSeek)
                fileStream.Position = 0;

            if (fileStream.Length <= 4 * 1024 * 1024)
            {
                using var httpClient = CreateGraphClient(accessToken);
                var uploadUrl = $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(driveId)}/root:{targetPath}:/content";
                using var content = new StreamContent(fileStream);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                using var response = await httpClient.PutAsync(uploadUrl, content, ct);
                var json = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                    return new StorageUploadResult { Success = false, Error = $"OneDrive upload hatasi ({(int)response.StatusCode}): {json}" };

                return ParseUploadResult(json, targetPath);
            }

            return await UploadLargeFileAsync(accessToken, driveId, targetPath, fileName, fileStream, ct);
        }
        catch (Exception ex)
        {
            return new StorageUploadResult
            {
                Success = false,
                Error = $"OneDrive upload hatasi: {ex.Message}"
            };
        }
    }

    public async Task<Stream> DownloadAsync(string fileId, CancellationToken ct = default)
    {
        var accessToken = await GetAccessTokenAsync(ct);
        var driveId = EnsureDriveId();
        using var httpClient = CreateGraphClient(accessToken);
        var url = $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(fileId)}/content";
        using var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var memory = new MemoryStream();
        await response.Content.CopyToAsync(memory, ct);
        memory.Position = 0;
        return memory;
    }

    public async Task<bool> DeleteAsync(string fileId, CancellationToken ct = default)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(ct);
            var driveId = EnsureDriveId();
            using var httpClient = CreateGraphClient(accessToken);
            var url = $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(fileId)}";
            using var response = await httpClient.DeleteAsync(url, ct);
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
            var accessToken = await GetAccessTokenAsync(ct);
            var driveId = EnsureDriveId();
            using var httpClient = CreateGraphClient(accessToken);
            var url = $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(fileId)}?$select=id";
            using var response = await httpClient.GetAsync(url, ct);
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
            var accessToken = await GetAccessTokenAsync(ct);
            var driveId = EnsureDriveId();
            using var httpClient = CreateGraphClient(accessToken);
            var url = $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(fileId)}?$select=id,@microsoft.graph.downloadUrl";
            using var response = await httpClient.GetAsync(url, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return null;

            return TryGetJsonString(json, "@microsoft.graph.downloadUrl");
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
            var accessToken = await GetAccessTokenAsync(ct);
            var driveId = EnsureDriveId();
            using var httpClient = CreateGraphClient(accessToken);
            var url = $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(driveId)}";
            using var response = await httpClient.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
                return (true, null);

            var json = await response.Content.ReadAsStringAsync(ct);
            return (false, $"OneDrive baglanti hatasi ({(int)response.StatusCode}): {json}");
        }
        catch (Exception ex)
        {
            return (false, $"OneDrive baglanti hatasi: {ex.Message}");
        }
    }

    /// <summary>Delegated mode can discover the current user's drive with /me/drive.</summary>
    public async Task<string?> DiscoverDriveIdAsync(CancellationToken ct = default)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(ct);
            using var httpClient = CreateGraphClient(accessToken);
            using var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me/drive?$select=id", ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            return response.IsSuccessStatusCode ? TryGetJsonString(json, "id") : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<StorageUploadResult> UploadLargeFileAsync(
        string accessToken,
        string driveId,
        string targetPath,
        string fileName,
        Stream fileStream,
        CancellationToken ct)
    {
        using var httpClient = CreateGraphClient(accessToken);
        var sessionUrl = $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(driveId)}/root:{targetPath}:/createUploadSession";
        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["item"] = new Dictionary<string, object?>
            {
                ["@microsoft.graph.conflictBehavior"] = "replace",
                ["name"] = fileName
            }
        });

        using var sessionContent = new StringContent(payload, Encoding.UTF8, "application/json");
        using var sessionResponse = await httpClient.PostAsync(sessionUrl, sessionContent, ct);
        var sessionJson = await sessionResponse.Content.ReadAsStringAsync(ct);
        if (!sessionResponse.IsSuccessStatusCode)
            return new StorageUploadResult { Success = false, Error = $"Upload session olusturulamadi ({(int)sessionResponse.StatusCode}): {sessionJson}" };

        var uploadUrl = TryGetJsonString(sessionJson, "uploadUrl");
        if (uploadUrl == null)
            return new StorageUploadResult { Success = false, Error = "Upload session olusturulamadi" };

        const int chunkSize = 5 * 1024 * 1024;
        var totalLength = fileStream.Length;
        if (fileStream.CanSeek)
            fileStream.Position = 0;

        var buffer = new byte[chunkSize];
        long uploaded = 0;
        while (uploaded < totalLength)
        {
            var read = await fileStream.ReadAsync(
                buffer.AsMemory(0, (int)Math.Min(chunkSize, totalLength - uploaded)), ct);
            if (read == 0)
                break;

            using var chunkContent = new ByteArrayContent(buffer, 0, read);
            chunkContent.Headers.ContentLength = read;
            chunkContent.Headers.ContentRange = new ContentRangeHeaderValue(uploaded, uploaded + read - 1, totalLength);

            using var chunkResponse = await httpClient.PutAsync(uploadUrl, chunkContent, ct);
            var chunkJson = await chunkResponse.Content.ReadAsStringAsync(ct);
            if (chunkResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                uploaded += read;
                continue;
            }

            if (chunkResponse.IsSuccessStatusCode)
                return ParseUploadResult(chunkJson, targetPath);

            return new StorageUploadResult { Success = false, Error = $"OneDrive chunk upload hatasi ({(int)chunkResponse.StatusCode}): {chunkJson}" };
        }

        return new StorageUploadResult { Success = false, Error = "OneDrive upload tamamlanamadi" };
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_credentials.ClientId))
            throw new InvalidOperationException("OneDrive ClientId bos");
        if (string.IsNullOrWhiteSpace(_credentials.ClientSecret))
            throw new InvalidOperationException("OneDrive ClientSecret bos");

        var tenantId = string.IsNullOrWhiteSpace(_credentials.TenantId) ? "common" : _credentials.TenantId;
        var tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

        Dictionary<string, string> form;
        if (_credentials.IsDelegated && !string.IsNullOrWhiteSpace(_credentials.RefreshToken))
        {
            form = new Dictionary<string, string>
            {
                ["client_id"] = _credentials.ClientId,
                ["client_secret"] = _credentials.ClientSecret,
                ["refresh_token"] = _credentials.RefreshToken,
                ["grant_type"] = "refresh_token",
                ["scope"] = "https://graph.microsoft.com/.default offline_access"
            };
        }
        else
        {
            form = new Dictionary<string, string>
            {
                ["client_id"] = _credentials.ClientId,
                ["client_secret"] = _credentials.ClientSecret,
                ["grant_type"] = "client_credentials",
                ["scope"] = "https://graph.microsoft.com/.default"
            };
        }

        using var httpClient = new HttpClient();
        using var response = await httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(form), ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OneDrive token alinamadi ({(int)response.StatusCode}): {json}");

        return TryGetJsonString(json, "access_token")
            ?? throw new InvalidOperationException("OneDrive token yanitinda access_token yok");
    }

    private string EnsureDriveId()
    {
        if (!string.IsNullOrWhiteSpace(_driveId))
            return _driveId;

        throw new InvalidOperationException("OneDrive DriveId yapilandirilmamis. Admin panelde DriveId girin.");
    }

    private static HttpClient CreateGraphClient(string accessToken)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return httpClient;
    }

    private StorageUploadResult ParseUploadResult(string json, string fallbackId)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new StorageUploadResult
        {
            Success = true,
            FileId = TryGetJsonString(root, "id") ?? fallbackId,
            FileUrl = TryGetJsonString(root, "webUrl"),
            FileSize = root.TryGetProperty("size", out var size) && size.TryGetInt64(out var value) ? value : 0
        };
    }

    private string BuildPath(string fileName, string? folder)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_folderId))
            parts.AddRange(_folderId.Split('/', StringSplitOptions.RemoveEmptyEntries));
        if (!string.IsNullOrWhiteSpace(folder))
            parts.AddRange(folder.Split('/', StringSplitOptions.RemoveEmptyEntries));
        parts.Add(fileName);

        return "/" + string.Join("/", parts.Select(Uri.EscapeDataString));
    }

    private static string? TryGetJsonString(string json, string propertyName)
    {
        using var doc = JsonDocument.Parse(json);
        return TryGetJsonString(doc.RootElement, propertyName);
    }

    private static string? TryGetJsonString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var property) ? property.GetString() : null;
}
