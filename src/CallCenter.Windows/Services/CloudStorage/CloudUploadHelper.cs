using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace CallCenter.Windows.Services.CloudStorage;

/// <summary>
/// Bulut depolamaya dosya yukleme helper'i.
/// CloudConfigForClientDto (API'den alinan decrypted config) ile calisir.
/// Her provider icin sadece upload mantigi — Download/Delete API tarafinda.
/// </summary>
public static class CloudUploadHelper
{
    /// <summary>
    /// Dosyayi musterinin bulut depolamasina yukle.
    /// </summary>
    /// <returns>(Basarili mi, Bulut dosya ID'si, Hata mesaji)</returns>
    public static async Task<(bool Success, string? FileId, string? Error)> UploadAsync(
        CloudConfigForClientDto config, Stream fileStream, string fileName,
        CancellationToken ct = default)
    {
        try
        {
            return config.ProviderTypeId switch
            {
                StorageProviders.Ids.AmazonS3 => await UploadToS3Async(config, false, fileStream, fileName, ct),
                StorageProviders.Ids.MinIO => await UploadToS3Async(config, true, fileStream, fileName, ct),
                StorageProviders.Ids.GoogleDrive => await UploadToGoogleDriveAsync(config, fileStream, fileName, ct),
                StorageProviders.Ids.OneDrive => await UploadToOneDriveAsync(config, fileStream, fileName, ct),
                StorageProviders.Ids.YandexDisk => await UploadToYandexAsync(config, fileStream, fileName, ct),
                StorageProviders.Ids.LocalDisk => (false, null, "LocalDisk islemi RecordingUploadService tarafindan yapilir"),
                _ => (false, null, $"Desteklenmeyen provider: {config.ProviderTypeId}")
            };
        }
        catch (Exception ex)
        {
            return (false, null, $"Upload hatasi: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════
    // AMAZON S3 / MinIO
    // ═══════════════════════════════════════

    private static async Task<(bool, string?, string?)> UploadToS3Async(
        CloudConfigForClientDto config, bool isMinIO, Stream fileStream, string fileName,
        CancellationToken ct)
    {
        var cred = config.Credentials;
        var bucketName = cred.GetValueOrDefault("BucketName", "");
        var accessKey = cred.GetValueOrDefault("AccessKey", "");
        var secretKey = cred.GetValueOrDefault("SecretKey", "");

        var s3Config = new AmazonS3Config();

        if (isMinIO && cred.TryGetValue("Endpoint", out var endpoint) && !string.IsNullOrEmpty(endpoint))
        {
            s3Config.ServiceURL = endpoint;
            s3Config.ForcePathStyle = true;
            if (cred.TryGetValue("UseSSL", out var useSsl) && useSsl == "false")
                s3Config.UseHttp = true;
        }
        else if (cred.TryGetValue("Region", out var region) && !string.IsNullOrEmpty(region))
        {
            s3Config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
        }
        else
        {
            s3Config.RegionEndpoint = RegionEndpoint.USEast1;
        }

        using var client = new AmazonS3Client(accessKey, secretKey, s3Config);

        // Key olustur: prefix/basePath/fileName
        var keyParts = new List<string>();
        if (cred.TryGetValue("Prefix", out var prefix) && !string.IsNullOrEmpty(prefix))
            keyParts.Add(prefix.TrimEnd('/'));
        if (!string.IsNullOrEmpty(config.BasePath))
            keyParts.Add(config.BasePath.Trim('/'));
        keyParts.Add(fileName);
        var key = string.Join("/", keyParts);

        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = fileStream,
            AutoCloseStream = false
        }, ct);

        return (true, key, null);
    }

    // ═══════════════════════════════════════
    // GOOGLE DRIVE
    // ═══════════════════════════════════════

    private static async Task<(bool, string?, string?)> UploadToGoogleDriveAsync(
        CloudConfigForClientDto config, Stream fileStream, string fileName,
        CancellationToken ct)
    {
        var cred = config.Credentials;

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = cred.GetValueOrDefault("ClientId", ""),
                ClientSecret = cred.GetValueOrDefault("ClientSecret", "")
            }
        });

        var tokenResponse = new TokenResponse { RefreshToken = cred.GetValueOrDefault("RefreshToken", "") };
        var credential = new UserCredential(flow, "user", tokenResponse);

        using var driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "CorpLynk"
        });

        var fileMetadata = new Google.Apis.Drive.v3.Data.File { Name = fileName };

        // Hedef klasor — ID veya isim olabilir
        cred.TryGetValue("FolderId", out var folderId);
        if (!string.IsNullOrEmpty(folderId))
        {
            // Google Drive folder ID'leri genelde 25+ karakter alfanumerik olur.
            // Kisa veya bosluk/Turkce karakter iceriyorsa isim olarak kabul et.
            var looksLikeId = folderId.Length >= 20 && !folderId.Contains(' ');
            if (!looksLikeId)
            {
                // Isim ile klasor ara, yoksa olustur
                folderId = await ResolveOrCreateFolderAsync(driveService, folderId, ct);
            }
        }

        // BasePath varsa FolderId icerisinde alt klasor olustur
        if (!string.IsNullOrEmpty(config.BasePath))
        {
            var subFolderName = config.BasePath.Trim('/');
            if (!string.IsNullOrEmpty(subFolderName))
            {
                folderId = await ResolveOrCreateFolderAsync(driveService, subFolderName, ct, folderId);
            }
        }

        if (!string.IsNullOrEmpty(folderId))
            fileMetadata.Parents = new List<string> { folderId };

        var request = driveService.Files.Create(fileMetadata, fileStream, "application/octet-stream");
        request.Fields = "id";
        var progress = await request.UploadAsync(ct);

        if (progress.Status == Google.Apis.Upload.UploadStatus.Failed)
            return (false, null, $"Google Drive upload hatasi: {progress.Exception?.Message}");

        return (true, request.ResponseBody?.Id, null);
    }

    /// <summary>
    /// Google Drive'da isimle klasor arar, yoksa olusturur. Folder ID dondurur.
    /// parentId verilirse o klasorun icinde arar/olusturur.
    /// </summary>
    private static async Task<string?> ResolveOrCreateFolderAsync(
        DriveService driveService, string folderName, CancellationToken ct, string? parentId = null)
    {
        // 1. Ara
        var listReq = driveService.Files.List();
        var query = $"name = '{folderName.Replace("'", "\\'")}' and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
        if (!string.IsNullOrEmpty(parentId))
            query += $" and '{parentId}' in parents";
        listReq.Q = query;
        listReq.Fields = "files(id, name)";
        listReq.PageSize = 1;
        var result = await listReq.ExecuteAsync(ct);

        if (result.Files?.Count > 0)
            return result.Files[0].Id;

        // 2. Yoksa olustur
        var folderMeta = new Google.Apis.Drive.v3.Data.File
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder"
        };
        if (!string.IsNullOrEmpty(parentId))
            folderMeta.Parents = new List<string> { parentId };
        var createReq = driveService.Files.Create(folderMeta);
        createReq.Fields = "id";
        var folder = await createReq.ExecuteAsync(ct);
        return folder?.Id;
    }

    // ═══════════════════════════════════════
    // ONEDRIVE (Microsoft Graph REST)
    // ═══════════════════════════════════════

    private static async Task<(bool, string?, string?)> UploadToOneDriveAsync(
        CloudConfigForClientDto config, Stream fileStream, string fileName,
        CancellationToken ct)
    {
        var cred = config.Credentials;
        string tenantId = cred.GetValueOrDefault("TenantId", "common") ?? "common";
        string driveId = cred.GetValueOrDefault("DriveId", "") ?? "";

        if (string.IsNullOrEmpty(driveId))
            return (false, null, "OneDrive DriveId yapilandirilmamis");

        string authMode = cred.GetValueOrDefault("AuthMode", "ClientCredential") ?? "ClientCredential";
        string refreshToken = cred.GetValueOrDefault("RefreshToken", "") ?? "";
        string clientId = cred.GetValueOrDefault("ClientId", "") ?? "";
        string clientSecret = cred.GetValueOrDefault("ClientSecret", "") ?? "";

        // Debug: credentials'da ne var kontrol
        var clientIdPreview = clientId[..Math.Min(clientId.Length, 10)];
        UploadDebugLog($"OneDrive creds: AuthMode={authMode}, ClientId={clientIdPreview}..., " +
            $"Secret={(string.IsNullOrEmpty(clientSecret) ? "BOS" : "DOLU")}, " +
            $"RefreshToken={(string.IsNullOrEmpty(refreshToken) ? "BOS" : "DOLU")}, " +
            $"DriveId={driveId}, TenantId={tenantId}, " +
            $"CredKeys=[{string.Join(",", cred.Keys)}]");

        var accessToken = await GetOneDriveAccessTokenAsync(
            tenantId, authMode, clientId, clientSecret, refreshToken, ct);
        var targetPath = BuildOneDriveTargetPath(config, cred, fileName);

        // 4MB'den kucuk: basit upload
        if (fileStream.Length <= 4 * 1024 * 1024)
        {
            using var httpClient = CreateGraphClient(accessToken);
            var uploadUrl = $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(driveId)}/root:{targetPath}:/content";
            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var response = await httpClient.PutAsync(uploadUrl, content, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                return (false, null, $"OneDrive upload hatasi ({(int)response.StatusCode}): {json}");

            return (true, TryGetJsonString(json, "id") ?? targetPath, null);
        }

        return await UploadLargeFileToOneDriveAsync(accessToken, driveId, targetPath, fileName, fileStream, ct);
    }

    private static async Task<string> GetOneDriveAccessTokenAsync(
        string tenantId,
        string authMode,
        string clientId,
        string clientSecret,
        string refreshToken,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("OneDrive ClientId bos");
        if (string.IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException("OneDrive ClientSecret bos");

        var tokenTenant = string.IsNullOrWhiteSpace(tenantId) ? "common" : tenantId;
        var tokenUrl = $"https://login.microsoftonline.com/{tokenTenant}/oauth2/v2.0/token";

        Dictionary<string, string> form;
        if (authMode == "Delegated" && !string.IsNullOrWhiteSpace(refreshToken))
        {
            form = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token",
                ["scope"] = "https://graph.microsoft.com/.default offline_access"
            };
        }
        else
        {
            form = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
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

    private static async Task<(bool, string?, string?)> UploadLargeFileToOneDriveAsync(
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
        var sessionResponse = await httpClient.PostAsync(sessionUrl, sessionContent, ct);
        var sessionJson = await sessionResponse.Content.ReadAsStringAsync(ct);
        if (!sessionResponse.IsSuccessStatusCode)
            return (false, null, $"OneDrive upload session hatasi ({(int)sessionResponse.StatusCode}): {sessionJson}");

        var uploadUrl = TryGetJsonString(sessionJson, "uploadUrl");
        if (uploadUrl == null)
            return (false, null, "OneDrive upload session olusturulamadi");

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

            var chunkResponse = await httpClient.PutAsync(uploadUrl, chunkContent, ct);
            var chunkJson = await chunkResponse.Content.ReadAsStringAsync(ct);
            if (chunkResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                uploaded += read;
                continue;
            }

            if (chunkResponse.IsSuccessStatusCode)
                return (true, TryGetJsonString(chunkJson, "id") ?? targetPath, null);

            return (false, null, $"OneDrive chunk upload hatasi ({(int)chunkResponse.StatusCode}): {chunkJson}");
        }

        return (false, null, "OneDrive upload tamamlanamadi");
    }

    private static HttpClient CreateGraphClient(string accessToken)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return httpClient;
    }

    private static string BuildOneDriveTargetPath(
        CloudConfigForClientDto config,
        IReadOnlyDictionary<string, string> credentials,
        string fileName)
    {
        var pathParts = new List<string>();
        if (credentials.TryGetValue("FolderId", out var folderId) && !string.IsNullOrWhiteSpace(folderId))
            pathParts.AddRange(folderId.Split('/', StringSplitOptions.RemoveEmptyEntries));
        if (!string.IsNullOrWhiteSpace(config.BasePath))
            pathParts.AddRange(config.BasePath.Split('/', StringSplitOptions.RemoveEmptyEntries));
        pathParts.Add(fileName);

        return "/" + string.Join("/", pathParts.Select(Uri.EscapeDataString));
    }

    private static string? TryGetJsonString(string json, string propertyName)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty(propertyName, out var property)
            ? property.GetString()
            : null;
    }

    // ═══════════════════════════════════════
    // YANDEX DISK (REST API — SDK yok)
    // ═══════════════════════════════════════

    private static async Task<(bool, string?, string?)> UploadToYandexAsync(
        CloudConfigForClientDto config, Stream fileStream, string fileName,
        CancellationToken ct)
    {
        const string apiBase = "https://cloud-api.yandex.net/v1/disk";
        var cred = config.Credentials;

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("OAuth", cred.GetValueOrDefault("OAuthToken", ""));

        // Disk yolu olustur
        var basePath = cred.GetValueOrDefault("BasePath", config.BasePath ?? "")?.TrimEnd('/').TrimStart('/');
        var diskPath = string.IsNullOrEmpty(basePath)
            ? $"/{fileName}"
            : $"/{basePath}/{fileName}";

        // Klasor olustur (yoksa)
        var folderPath = diskPath[..diskPath.LastIndexOf('/')];
        if (!string.IsNullOrEmpty(folderPath) && folderPath != "/")
        {
            var parts = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentPath = "";
            foreach (var part in parts)
            {
                currentPath += "/" + part;
                var encodedCheck = Uri.EscapeDataString(currentPath);
                var check = await httpClient.GetAsync($"{apiBase}/resources?path={encodedCheck}", ct);
                if (!check.IsSuccessStatusCode)
                    await httpClient.PutAsync($"{apiBase}/resources?path={encodedCheck}", null, ct);
            }
        }

        // Upload URL al
        var encodedPath = Uri.EscapeDataString(diskPath);
        var urlResponse = await httpClient.GetAsync(
            $"{apiBase}/resources/upload?path={encodedPath}&overwrite=true", ct);

        if (!urlResponse.IsSuccessStatusCode)
        {
            var err = await urlResponse.Content.ReadAsStringAsync(ct);
            return (false, null, $"Yandex upload URL hatasi: {err}");
        }

        var urlJson = await urlResponse.Content.ReadAsStringAsync(ct);
        var uploadUrl = JsonDocument.Parse(urlJson).RootElement.GetProperty("href").GetString();

        if (string.IsNullOrEmpty(uploadUrl))
            return (false, null, "Yandex upload URL alinamadi");

        // Dosyayi yukle
        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var uploadResponse = await httpClient.PutAsync(uploadUrl, content, ct);

        if (!uploadResponse.IsSuccessStatusCode)
            return (false, null, $"Yandex upload hatasi: HTTP {uploadResponse.StatusCode}");

        return (true, diskPath, null);
    }

    private static void UploadDebugLog(string msg)
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CorpLynk", "upload-debug.log");
            File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}");
        }
        catch { }
    }
}
