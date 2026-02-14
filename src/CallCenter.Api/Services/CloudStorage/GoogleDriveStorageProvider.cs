using CallCenter.Shared.Enums;
using CallCenter.Shared.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;

namespace CallCenter.Api.Services.CloudStorage;

/// <summary>
/// Google Drive depolama provider'i.
/// OAuth2 refresh token ile calisir — access token otomatik yenilenir.
/// </summary>
public class GoogleDriveStorageProvider : ICloudStorageProvider, IDisposable
{
    private readonly DriveService _driveService;
    private readonly string? _folderId;

    public int ProviderTypeId => StorageProviders.Ids.GoogleDrive;

    public GoogleDriveStorageProvider(GoogleDriveCredentials credentials)
    {
        _folderId = credentials.FolderId;

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = credentials.ClientId,
                ClientSecret = credentials.ClientSecret
            }
        });

        var tokenResponse = new TokenResponse { RefreshToken = credentials.RefreshToken };
        var credential = new UserCredential(flow, "user", tokenResponse);

        _driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "CallCenter"
        });
    }

    public async Task<StorageUploadResult> UploadAsync(Stream fileStream, string fileName,
        string? folder = null, CancellationToken ct = default)
    {
        try
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName
            };

            // Klasor belirleme
            var targetFolderId = _folderId;
            if (!string.IsNullOrEmpty(folder))
            {
                targetFolderId = await GetOrCreateFolderAsync(folder, _folderId, ct);
            }

            if (!string.IsNullOrEmpty(targetFolderId))
            {
                fileMetadata.Parents = new List<string> { targetFolderId };
            }

            var request = _driveService.Files.Create(fileMetadata, fileStream, "application/octet-stream");
            request.Fields = "id, size, webViewLink";

            var progress = await request.UploadAsync(ct);

            if (progress.Status == UploadStatus.Failed)
            {
                return new StorageUploadResult
                {
                    Success = false,
                    Error = $"Google Drive upload hatasi: {progress.Exception?.Message}"
                };
            }

            var file = request.ResponseBody;
            return new StorageUploadResult
            {
                Success = true,
                FileId = file.Id,
                FileUrl = file.WebViewLink,
                FileSize = file.Size ?? 0
            };
        }
        catch (Exception ex)
        {
            return new StorageUploadResult
            {
                Success = false,
                Error = $"Google Drive upload hatasi: {ex.Message}"
            };
        }
    }

    public async Task<Stream> DownloadAsync(string fileId, CancellationToken ct = default)
    {
        var request = _driveService.Files.Get(fileId);
        var stream = new MemoryStream();
        await request.DownloadAsync(stream, ct);
        stream.Position = 0;
        return stream;
    }

    public async Task<bool> DeleteAsync(string fileId, CancellationToken ct = default)
    {
        try
        {
            await _driveService.Files.Delete(fileId).ExecuteAsync(ct);
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
            var request = _driveService.Files.Get(fileId);
            request.Fields = "id";
            await request.ExecuteAsync(ct);
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
        // Google Drive dosyalari icin webContentLink kullanilabilir
        try
        {
            var request = _driveService.Files.Get(fileId);
            request.Fields = "webContentLink";
            var file = await request.ExecuteAsync(ct);
            return file.WebContentLink;
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
            // "About" sorgusu ile baglanti testi
            var aboutRequest = _driveService.About.Get();
            aboutRequest.Fields = "user";
            var about = await aboutRequest.ExecuteAsync(ct);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Google Drive baglanti hatasi: {ex.Message}");
        }
    }

    /// <summary>Klasor yoksa olusturur, varsa ID'sini doner</summary>
    private async Task<string> GetOrCreateFolderAsync(string folderName, string? parentId,
        CancellationToken ct)
    {
        // Mevcut klasoru ara
        var query = $"name = '{folderName}' and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
        if (!string.IsNullOrEmpty(parentId))
            query += $" and '{parentId}' in parents";

        var listRequest = _driveService.Files.List();
        listRequest.Q = query;
        listRequest.Fields = "files(id)";
        var result = await listRequest.ExecuteAsync(ct);

        if (result.Files.Count > 0)
            return result.Files[0].Id;

        // Olustur
        var folderMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder"
        };

        if (!string.IsNullOrEmpty(parentId))
            folderMetadata.Parents = new List<string> { parentId };

        var createRequest = _driveService.Files.Create(folderMetadata);
        createRequest.Fields = "id";
        var folder = await createRequest.ExecuteAsync(ct);
        return folder.Id;
    }

    public void Dispose()
    {
        _driveService?.Dispose();
    }
}
