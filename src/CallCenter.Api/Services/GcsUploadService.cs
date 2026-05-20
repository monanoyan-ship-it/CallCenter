using Google.Cloud.Storage.V1;

namespace CallCenter.Api.Services;

/// <summary>
/// Google Cloud Storage gorsel yukleme servisi.
/// Cloud Run'da ADC (Application Default Credentials) otomatik calisir.
/// Lokalde: gcloud auth application-default login
/// </summary>
public class GcsUploadService
{
    private readonly string _bucketName;
    private readonly string _publicBaseUrl;
    private readonly StorageClient? _client;
    private readonly ILogger<GcsUploadService> _logger;

    public GcsUploadService(IConfiguration config, ILogger<GcsUploadService> logger)
    {
        _bucketName = config["Gcs:BucketName"] ?? "corplynk-salon-assets";
        _publicBaseUrl = config["Gcs:PublicBaseUrl"] ?? $"https://storage.googleapis.com/{_bucketName}";
        _logger = logger;

        try
        {
            _client = StorageClient.Create();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GCS client olusturulamadi. Gorsel yukleme devre disi.");
        }
    }

    public bool IsConfigured => _client != null;

    /// <summary>
    /// Gorsel yukler ve public URL doner.
    /// path: "salons/{customerId}/banner-1.jpg" gibi
    /// </summary>
    public async Task<(string? Url, string? Error)> UploadAsync(Stream fileStream, string path, string contentType)
    {
        if (_client == null)
            return (null, "GCS yapilandirilmamis. gcloud auth application-default login calistirin.");

        try
        {
            await _client.UploadObjectAsync(_bucketName, path, contentType, fileStream);
            var url = $"{_publicBaseUrl}/{path}";
            return (url, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GCS upload hatasi. Path={Path}", path);
            return (null, ex.Message);
        }
    }

    public async Task<bool> DeleteAsync(string path)
    {
        if (_client == null) return false;

        try
        {
            await _client.DeleteObjectAsync(_bucketName, path);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GCS silme hatasi. Path={Path}", path);
            return false;
        }
    }

    public string? TryGetObjectPath(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return trimmed.TrimStart('/');

        var publicBase = _publicBaseUrl.TrimEnd('/');
        if (trimmed.StartsWith($"{publicBase}/", StringComparison.OrdinalIgnoreCase))
            return Uri.UnescapeDataString(trimmed[(publicBase.Length + 1)..]);

        if (!string.Equals(uri.Host, "storage.googleapis.com", StringComparison.OrdinalIgnoreCase))
            return null;

        var path = uri.AbsolutePath.TrimStart('/');
        if (path.StartsWith($"{_bucketName}/", StringComparison.OrdinalIgnoreCase))
            path = path[(_bucketName.Length + 1)..];

        return Uri.UnescapeDataString(path);
    }
}
