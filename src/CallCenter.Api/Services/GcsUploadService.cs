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
    private readonly string _localUploadRoot;
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly StorageClient? _client;
    private readonly ILogger<GcsUploadService> _logger;

    public GcsUploadService(
        IConfiguration config,
        IWebHostEnvironment env,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GcsUploadService> logger)
    {
        _bucketName = config["Gcs:BucketName"] ?? "corplynk-salon-assets";
        _publicBaseUrl = config["Gcs:PublicBaseUrl"] ?? $"https://storage.googleapis.com/{_bucketName}";
        _env = env;
        _httpContextAccessor = httpContextAccessor;
        _localUploadRoot = Path.Combine(_env.ContentRootPath, "Data", "Uploads");
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
        {
            if (!_env.IsDevelopment())
                return (null, "GCS yapilandirilmamis. gcloud auth application-default login calistirin.");

            return await UploadLocalAsync(fileStream, path);
        }

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
        if (_client == null)
        {
            if (!_env.IsDevelopment()) return false;

            var localPath = TryResolveLocalPath(path);
            if (localPath == null || !File.Exists(localPath)) return false;
            File.Delete(localPath);
            return true;
        }

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
            return trimmed.TrimStart('/').StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)
                ? trimmed.TrimStart('/')["uploads/".Length..]
                : trimmed.TrimStart('/');

        if (uri.AbsolutePath.TrimStart('/').StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            return Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'))["uploads/".Length..];

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

    private async Task<(string? Url, string? Error)> UploadLocalAsync(Stream fileStream, string path)
    {
        try
        {
            path = path.Replace('\\', '/').TrimStart('/');
            var fullPath = Path.GetFullPath(Path.Combine(_localUploadRoot, path));
            var root = Path.GetFullPath(_localUploadRoot);
            if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return (null, "Gecersiz dosya yolu.");

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await using (var output = File.Create(fullPath))
                await fileStream.CopyToAsync(output);

            return ($"{ResolveRequestBaseUrl()}/uploads/{path}", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lokal upload hatasi. Path={Path}", path);
            return (null, ex.Message);
        }
    }

    private string ResolveRequestBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null) return "http://localhost:5041";
        return $"{request.Scheme}://{request.Host}";
    }

    private string? TryResolveLocalPath(string path)
    {
        path = path.Replace('\\', '/').TrimStart('/');
        if (path.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            path = path["uploads/".Length..];

        var fullPath = Path.GetFullPath(Path.Combine(_localUploadRoot, path));
        var root = Path.GetFullPath(_localUploadRoot);
        return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? fullPath : null;
    }
}
