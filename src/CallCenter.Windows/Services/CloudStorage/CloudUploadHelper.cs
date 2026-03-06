using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Azure.Identity;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Graph;
using Microsoft.Graph.Models;

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

        // Hedef klasor
        cred.TryGetValue("FolderId", out var folderId);
        if (!string.IsNullOrEmpty(folderId))
            fileMetadata.Parents = new List<string> { folderId };

        var request = driveService.Files.Create(fileMetadata, fileStream, "application/octet-stream");
        request.Fields = "id";
        var progress = await request.UploadAsync(ct);

        if (progress.Status == Google.Apis.Upload.UploadStatus.Failed)
            return (false, null, $"Google Drive upload hatasi: {progress.Exception?.Message}");

        return (true, request.ResponseBody?.Id, null);
    }

    // ═══════════════════════════════════════
    // ONEDRIVE (Microsoft Graph)
    // ═══════════════════════════════════════

    private static async Task<(bool, string?, string?)> UploadToOneDriveAsync(
        CloudConfigForClientDto config, Stream fileStream, string fileName,
        CancellationToken ct)
    {
        var cred = config.Credentials;
        var tenantId = cred.GetValueOrDefault("TenantId", "common");
        var driveId = cred.GetValueOrDefault("DriveId", "");

        if (string.IsNullOrEmpty(driveId))
            return (false, null, "OneDrive DriveId yapilandirilmamis");

        var credential = new ClientSecretCredential(
            tenantId,
            cred.GetValueOrDefault("ClientId", ""),
            cred.GetValueOrDefault("ClientSecret", ""));

        var graphClient = new GraphServiceClient(credential);

        // Dosya yolu olustur
        var pathParts = new List<string>();
        if (cred.TryGetValue("FolderId", out var folderId) && !string.IsNullOrEmpty(folderId))
            pathParts.Add(folderId);
        if (!string.IsNullOrEmpty(config.BasePath))
            pathParts.Add(config.BasePath.Trim('/'));
        pathParts.Add(fileName);
        var targetPath = "/" + string.Join("/", pathParts);

        // 4MB'den kucuk: basit upload
        if (fileStream.Length <= 4 * 1024 * 1024)
        {
            var driveItem = await graphClient.Drives[driveId]
                .Root
                .ItemWithPath(targetPath)
                .Content
                .PutAsync(fileStream, cancellationToken: ct);

            return (true, driveItem?.Id, null);
        }

        // Buyuk dosya: upload session
        var uploadSession = await graphClient.Drives[driveId]
            .Root
            .ItemWithPath(targetPath)
            .CreateUploadSession
            .PostAsync(new Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession.CreateUploadSessionPostRequestBody
            {
                Item = new DriveItemUploadableProperties { Name = fileName }
            }, cancellationToken: ct);

        if (uploadSession?.UploadUrl == null)
            return (false, null, "OneDrive upload session olusturulamadi");

        const int chunkSize = 5 * 1024 * 1024;
        var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, fileStream, chunkSize);
        var result = await fileUploadTask.UploadAsync(cancellationToken: ct);

        if (result.UploadSucceeded && result.ItemResponse is DriveItem item)
            return (true, item.Id, null);

        return (false, null, "OneDrive upload basarisiz");
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
}
