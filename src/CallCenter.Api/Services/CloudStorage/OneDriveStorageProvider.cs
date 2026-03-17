using Azure.Identity;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Interfaces;
using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.Content;
using Microsoft.Graph.Models;

namespace CallCenter.Api.Services.CloudStorage;

/// <summary>
/// Microsoft OneDrive (Microsoft 365) depolama provider'i.
/// Microsoft Graph API ile calisir. OAuth2 refresh token kullanir.
/// </summary>
public class OneDriveStorageProvider : ICloudStorageProvider
{
    private readonly GraphServiceClient _graphClient;
    private readonly string? _driveId;
    private readonly string? _folderId;

    public int ProviderTypeId => StorageProviders.Ids.OneDrive;

    public OneDriveStorageProvider(OneDriveCredentials credentials)
    {
        _driveId = credentials.DriveId;
        _folderId = credentials.FolderId;

        if (credentials.IsDelegated && !string.IsNullOrEmpty(credentials.RefreshToken))
        {
            // Delegated auth: Refresh token ile access token alinir (kolay/orta mod)
            var tokenCredential = new OneDriveRefreshTokenCredential(
                credentials.ClientId,
                credentials.ClientSecret,
                credentials.RefreshToken,
                credentials.TenantId);
            _graphClient = new GraphServiceClient(tokenCredential);
        }
        else
        {
            // Application auth: Client credential (gelismis mod)
            var credential = new ClientSecretCredential(
                credentials.TenantId ?? "common",
                credentials.ClientId,
                credentials.ClientSecret);
            _graphClient = new GraphServiceClient(credential);
        }
    }

    public async Task<StorageUploadResult> UploadAsync(Stream fileStream, string fileName,
        string? folder = null, CancellationToken ct = default)
    {
        try
        {
            var targetPath = BuildPath(fileName, folder);

            // 4MB'den kucuk dosyalar icin basit upload
            if (fileStream.Length <= 4 * 1024 * 1024)
            {
                var driveItem = await GetDriveRequestBuilder()
                    .Root
                    .ItemWithPath(targetPath)
                    .Content
                    .PutAsync(fileStream, cancellationToken: ct);

                return new StorageUploadResult
                {
                    Success = true,
                    FileId = driveItem?.Id,
                    FileUrl = driveItem?.WebUrl,
                    FileSize = driveItem?.Size ?? 0
                };
            }

            // Buyuk dosyalar icin upload session
            var uploadSession = await GetDriveRequestBuilder()
                .Root
                .ItemWithPath(targetPath)
                .CreateUploadSession
                .PostAsync(new Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession.CreateUploadSessionPostRequestBody
                {
                    Item = new DriveItemUploadableProperties
                    {
                        Name = fileName
                    }
                }, cancellationToken: ct);

            if (uploadSession?.UploadUrl == null)
            {
                return new StorageUploadResult { Success = false, Error = "Upload session olusturulamadi" };
            }

            // 5MB chunk'lar ile upload
            const int chunkSize = 5 * 1024 * 1024;
            var provider = new Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession.CreateUploadSessionPostRequestBody();
            var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, fileStream, chunkSize);
            var result = await fileUploadTask.UploadAsync(cancellationToken: ct);

            if (result.UploadSucceeded && result.ItemResponse is DriveItem item)
            {
                return new StorageUploadResult
                {
                    Success = true,
                    FileId = item.Id,
                    FileUrl = item.WebUrl,
                    FileSize = item.Size ?? 0
                };
            }

            return new StorageUploadResult { Success = false, Error = "OneDrive upload basarisiz" };
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
        var stream = await GetDriveRequestBuilder()
            .Items[fileId]
            .Content
            .GetAsync(cancellationToken: ct);

        return stream ?? Stream.Null;
    }

    public async Task<bool> DeleteAsync(string fileId, CancellationToken ct = default)
    {
        try
        {
            await GetDriveRequestBuilder()
                .Items[fileId]
                .DeleteAsync(cancellationToken: ct);
            return true;
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
            await GetDriveRequestBuilder()
                .Items[fileId]
                .GetAsync(cancellationToken: ct);
            return true;
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
            var item = await GetDriveRequestBuilder()
                .Items[fileId]
                .GetAsync(r => r.QueryParameters.Select = new[] { "id", "@microsoft.graph.downloadUrl" },
                    cancellationToken: ct);

            // @microsoft.graph.downloadUrl gecici bir indirme URL'i doner
            if (item?.AdditionalData?.TryGetValue("@microsoft.graph.downloadUrl", out var url) == true)
            {
                return url?.ToString();
            }
            return null;
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
            var drive = await GetDriveRequestBuilder().GetAsync(cancellationToken: ct);
            return (drive != null, null);
        }
        catch (Exception ex)
        {
            return (false, $"OneDrive baglanti hatasi: {ex.Message}");
        }
    }

    private Microsoft.Graph.Drives.Item.DriveItemRequestBuilder GetDriveRequestBuilder()
    {
        if (!string.IsNullOrEmpty(_driveId))
        {
            return _graphClient.Drives[_driveId];
        }
        throw new InvalidOperationException("OneDrive DriveId yapilandirilmamis. Admin panelde DriveId girin.");
    }

    /// <summary>Delegated modda /me/drive ile otomatik drive kesfeder.</summary>
    public async Task<string?> DiscoverDriveIdAsync(CancellationToken ct = default)
    {
        try
        {
            var drive = await _graphClient.Me.Drive.GetAsync(cancellationToken: ct);
            return drive?.Id;
        }
        catch { return null; }
    }

    private string BuildPath(string fileName, string? folder)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(_folderId)) parts.Add(_folderId);
        if (!string.IsNullOrEmpty(folder)) parts.Add(folder.Trim('/'));
        parts.Add(fileName);
        return "/" + string.Join("/", parts);
    }
}
