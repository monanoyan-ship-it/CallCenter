using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Interfaces;

namespace CallCenter.Api.Services.CloudStorage;

/// <summary>
/// Amazon S3 ve MinIO (S3-uyumlu) depolama provider'i.
/// MinIO icin ForcePathStyle = true ve custom endpoint kullanilir.
/// </summary>
public class S3StorageProvider : ICloudStorageProvider, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;
    private readonly string? _prefix;
    private readonly bool _isMinIO;

    public int ProviderTypeId { get; }

    public S3StorageProvider(S3Credentials credentials, bool isMinIO)
    {
        _isMinIO = isMinIO;
        ProviderTypeId = isMinIO ? StorageProviders.Ids.MinIO : StorageProviders.Ids.AmazonS3;
        _bucketName = credentials.BucketName;
        _prefix = credentials.Prefix?.TrimEnd('/');

        var config = new AmazonS3Config();

        if (isMinIO && !string.IsNullOrEmpty(credentials.Endpoint))
        {
            // MinIO: custom endpoint + ForcePathStyle
            config.ServiceURL = credentials.Endpoint;
            config.ForcePathStyle = true;
            if (!credentials.UseSSL)
            {
                config.UseHttp = true;
            }
        }
        else if (!string.IsNullOrEmpty(credentials.Region))
        {
            // Amazon S3: region-based endpoint
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(credentials.Region);
        }
        else
        {
            // Varsayilan: us-east-1
            config.RegionEndpoint = RegionEndpoint.USEast1;
        }

        _client = new AmazonS3Client(credentials.AccessKey, credentials.SecretKey, config);
    }

    public async Task<StorageUploadResult> UploadAsync(Stream fileStream, string fileName,
        string? folder = null, CancellationToken ct = default)
    {
        try
        {
            var key = BuildKey(fileName, folder);

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = fileStream,
                AutoCloseStream = false
            };

            await _client.PutObjectAsync(request, ct);

            return new StorageUploadResult
            {
                Success = true,
                FileId = key,
                FileSize = fileStream.Length
            };
        }
        catch (Exception ex)
        {
            return new StorageUploadResult
            {
                Success = false,
                Error = $"S3 upload hatasi: {ex.Message}"
            };
        }
    }

    public async Task<Stream> DownloadAsync(string fileId, CancellationToken ct = default)
    {
        var response = await _client.GetObjectAsync(_bucketName, fileId, ct);
        return response.ResponseStream;
    }

    public async Task<bool> DeleteAsync(string fileId, CancellationToken ct = default)
    {
        try
        {
            await _client.DeleteObjectAsync(_bucketName, fileId, ct);
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
            await _client.GetObjectMetadataAsync(_bucketName, fileId, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<string?> GetDownloadUrlAsync(string fileId, TimeSpan expiry,
        CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileId,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        // GetPreSignedURL senkron ama hizli (imza hesaplama)
        return await Task.FromResult(_client.GetPreSignedURL(request));
    }

    public async Task<(bool Success, string? Error)> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            // Bucket varligini kontrol et
            await _client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucketName,
                MaxKeys = 1
            }, ct);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"S3 baglanti hatasi: {ex.Message}");
        }
    }

    private string BuildKey(string fileName, string? folder)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(_prefix)) parts.Add(_prefix);
        if (!string.IsNullOrEmpty(folder)) parts.Add(folder.Trim('/'));
        parts.Add(fileName);
        return string.Join("/", parts);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
