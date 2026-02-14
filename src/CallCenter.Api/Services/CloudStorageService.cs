using System.Text.Json;
using CallCenter.Api.Services.CloudStorage;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

/// <summary>
/// Bulut depolama is mantigi servisi.
/// Musterinin default config'ini bulur, factory ile provider olusturur, islemi yapar.
/// Credential'lar DB'de AES-256 sifreli saklanir.
/// </summary>
public class CloudStorageService : ICloudStorageService
{
    private readonly AppDbContext _db;
    private readonly CloudStorageFactory _factory;
    private readonly AesEncryptionService _encryption;

    public CloudStorageService(AppDbContext db, CloudStorageFactory factory, AesEncryptionService encryption)
    {
        _db = db;
        _factory = factory;
        _encryption = encryption;
    }

    // ═══════════════════════════════════════════════════════════════
    // DOSYA ISLEMLERI
    // ═══════════════════════════════════════════════════════════════

    public async Task<StorageUploadResult> UploadRecordingAsync(int customerId, Stream fileStream,
        string fileName, CancellationToken ct = default)
    {
        var config = await GetDefaultConfigAsync(customerId);
        if (config == null)
            return new StorageUploadResult { Success = false, Error = "Musteri icin depolama yapilandirilmamis" };

        var provider = _factory.Create(config);
        try
        {
            return await provider.UploadAsync(fileStream, fileName, config.BasePath, ct);
        }
        finally
        {
            if (provider is IDisposable d) d.Dispose();
        }
    }

    public async Task<Stream?> DownloadRecordingAsync(int customerId, string fileId, CancellationToken ct = default)
    {
        var config = await GetDefaultConfigAsync(customerId);
        if (config == null) return null;

        var provider = _factory.Create(config);
        try
        {
            return await provider.DownloadAsync(fileId, ct);
        }
        catch
        {
            if (provider is IDisposable d) d.Dispose();
            return null;
        }
    }

    public async Task<bool> DeleteRecordingAsync(int customerId, string fileId, CancellationToken ct = default)
    {
        var config = await GetDefaultConfigAsync(customerId);
        if (config == null) return false;

        var provider = _factory.Create(config);
        try
        {
            return await provider.DeleteAsync(fileId, ct);
        }
        finally
        {
            if (provider is IDisposable d) d.Dispose();
        }
    }

    public async Task<string?> GetRecordingUrlAsync(int customerId, string fileId, CancellationToken ct = default)
    {
        var config = await GetDefaultConfigAsync(customerId);
        if (config == null) return null;

        var provider = _factory.Create(config);
        try
        {
            return await provider.GetDownloadUrlAsync(fileId, TimeSpan.FromMinutes(30), ct);
        }
        finally
        {
            if (provider is IDisposable d) d.Dispose();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // CONFIG CRUD
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<StorageConfigListDto>> GetConfigsAsync(int? customerId = null)
    {
        var query = _db.CustomerStorageConfigs
            .Include(c => c.Customer)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(c => c.CustomerId == customerId.Value);

        return await query
            .OrderBy(c => c.Customer.Name)
            .ThenBy(c => c.ProviderTypeId)
            .Select(c => MapToListDto(c))
            .ToListAsync();
    }

    public async Task<StorageConfigDetailDto?> GetConfigByIdAsync(int configId)
    {
        var config = await _db.CustomerStorageConfigs
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == configId);

        if (config == null) return null;

        var listDto = MapToListDto(config);
        var detail = new StorageConfigDetailDto
        {
            Id = listDto.Id,
            Uid = listDto.Uid,
            CustomerId = listDto.CustomerId,
            CustomerName = listDto.CustomerName,
            ProviderTypeId = listDto.ProviderTypeId,
            ProviderName = listDto.ProviderName,
            ProviderIcon = listDto.ProviderIcon,
            ProviderCss = listDto.ProviderCss,
            BasePath = listDto.BasePath,
            IsActive = listDto.IsActive,
            IsDefault = listDto.IsDefault,
            LastTestedAt = listDto.LastTestedAt,
            LastTestSuccess = listDto.LastTestSuccess,
            LastTestError = listDto.LastTestError,
            CreatedAt = listDto.CreatedAt,
            MaskedCredentials = GetMaskedCredentials(config)
        };

        return detail;
    }

    public async Task<(bool Success, int? Id, string? Error)> CreateConfigAsync(StorageConfigCreateDto dto)
    {
        // Provider gecerli mi?
        var provider = StorageProviders.GetById(dto.ProviderTypeId);
        if (provider == null)
            return (false, null, $"Gecersiz provider tipi: {dto.ProviderTypeId}");

        // Musteri var mi?
        var customer = await _db.Customers.FindAsync(dto.CustomerId);
        if (customer == null)
            return (false, null, "Musteri bulunamadi");

        // Credential JSON olustur ve sifrele
        var credentialsJson = BuildCredentialsJson(dto);
        var encryptedCredentials = _encryption.Encrypt(credentialsJson);

        // Default ise digerleri default olmaktan cikar
        if (dto.IsDefault)
        {
            var existingDefaults = await _db.CustomerStorageConfigs
                .Where(c => c.CustomerId == dto.CustomerId && c.IsDefault)
                .ToListAsync();
            foreach (var ed in existingDefaults)
                ed.IsDefault = false;
        }

        var config = new CustomerStorageConfig
        {
            CustomerId = dto.CustomerId,
            ProviderTypeId = dto.ProviderTypeId,
            EncryptedCredentials = encryptedCredentials,
            BasePath = dto.BasePath,
            IsDefault = dto.IsDefault,
            IsActive = true
        };

        _db.CustomerStorageConfigs.Add(config);
        await _db.SaveChangesAsync();

        return (true, config.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateConfigAsync(int configId, StorageConfigUpdateDto dto)
    {
        var config = await _db.CustomerStorageConfigs.FindAsync(configId);
        if (config == null)
            return (false, "Config bulunamadi");

        if (dto.ProviderTypeId.HasValue)
        {
            if (StorageProviders.GetById(dto.ProviderTypeId.Value) == null)
                return (false, $"Gecersiz provider tipi: {dto.ProviderTypeId}");
            config.ProviderTypeId = dto.ProviderTypeId.Value;
        }

        if (dto.BasePath != null) config.BasePath = dto.BasePath;
        if (dto.IsActive.HasValue) config.IsActive = dto.IsActive.Value;

        // Default degisikligi
        if (dto.IsDefault.HasValue && dto.IsDefault.Value && !config.IsDefault)
        {
            var existingDefaults = await _db.CustomerStorageConfigs
                .Where(c => c.CustomerId == config.CustomerId && c.IsDefault && c.Id != configId)
                .ToListAsync();
            foreach (var ed in existingDefaults)
                ed.IsDefault = false;
            config.IsDefault = true;
        }
        else if (dto.IsDefault.HasValue)
        {
            config.IsDefault = dto.IsDefault.Value;
        }

        // Credential guncelleme (herhangi bir credential alani doluysa yeniden sifrele)
        if (HasAnyCredentialField(dto))
        {
            var existingJson = _encryption.Decrypt(config.EncryptedCredentials);
            var mergedJson = MergeCredentialsJson(existingJson, dto);
            config.EncryptedCredentials = _encryption.Encrypt(mergedJson);
        }

        config.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteConfigAsync(int configId)
    {
        var config = await _db.CustomerStorageConfigs.FindAsync(configId);
        if (config == null)
            return (false, "Config bulunamadi");

        _db.CustomerStorageConfigs.Remove(config);
        await _db.SaveChangesAsync();

        return (true, null);
    }

    // ═══════════════════════════════════════════════════════════════
    // BAGLANTI TESTI
    // ═══════════════════════════════════════════════════════════════

    public async Task<StorageTestResultDto> TestConnectionAsync(int configId, CancellationToken ct = default)
    {
        var config = await _db.CustomerStorageConfigs.FindAsync(configId);
        if (config == null)
            return new StorageTestResultDto { Success = false, Error = "Config bulunamadi", TestedAt = DateTime.UtcNow };

        var provider = _factory.Create(config);
        try
        {
            var (success, error) = await provider.TestConnectionAsync(ct);

            // Test sonucunu DB'ye kaydet
            config.LastTestedAt = DateTime.UtcNow;
            config.LastTestSuccess = success;
            config.LastTestError = error;
            await _db.SaveChangesAsync();

            return new StorageTestResultDto
            {
                Success = success,
                Error = error,
                TestedAt = config.LastTestedAt.Value
            };
        }
        finally
        {
            if (provider is IDisposable d) d.Dispose();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // PROVIDER BILGISI
    // ═══════════════════════════════════════════════════════════════

    public List<StorageProviderInfoDto> GetAvailableProviders()
    {
        return StorageProviders.All.Select(p => new StorageProviderInfoDto
        {
            Id = p.Id,
            SystemName = p.SystemName,
            Description = p.Description,
            Icon = p.Icon,
            CssClass = p.CssClass,
            RequiresOAuth = p.Id is StorageProviders.Ids.GoogleDrive or StorageProviders.Ids.OneDrive,
            RequiredFields = GetRequiredFields(p.Id)
        }).ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPER METODLAR
    // ═══════════════════════════════════════════════════════════════

    private async Task<CustomerStorageConfig?> GetDefaultConfigAsync(int customerId)
    {
        return await _db.CustomerStorageConfigs
            .Where(c => c.CustomerId == customerId && c.IsActive)
            .OrderByDescending(c => c.IsDefault)  // Once default olani
            .FirstOrDefaultAsync();
    }

    private static StorageConfigListDto MapToListDto(CustomerStorageConfig c)
    {
        var provider = StorageProviders.GetById(c.ProviderTypeId);
        return new StorageConfigListDto
        {
            Id = c.Id,
            Uid = c.Uid,
            CustomerId = c.CustomerId,
            CustomerName = c.Customer?.Name ?? "",
            ProviderTypeId = c.ProviderTypeId,
            ProviderName = provider?.Description ?? provider?.SystemName ?? "Bilinmiyor",
            ProviderIcon = provider?.Icon ?? "bi-cloud",
            ProviderCss = provider?.CssClass ?? "",
            BasePath = c.BasePath,
            IsActive = c.IsActive,
            IsDefault = c.IsDefault,
            LastTestedAt = c.LastTestedAt,
            LastTestSuccess = c.LastTestSuccess,
            LastTestError = c.LastTestError,
            CreatedAt = c.CreatedAt
        };
    }

    private Dictionary<string, string> GetMaskedCredentials(CustomerStorageConfig config)
    {
        try
        {
            var json = _encryption.Decrypt(config.EncryptedCredentials);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dict == null) return new();

            // Hassas alanlari maskele
            var masked = new Dictionary<string, string>();
            foreach (var (key, value) in dict)
            {
                if (string.IsNullOrEmpty(value))
                {
                    masked[key] = "";
                    continue;
                }

                // Secret, Token, Password iceren alanlar maskelenir
                if (key.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("Password", StringComparison.OrdinalIgnoreCase))
                {
                    masked[key] = value.Length > 8
                        ? value[..4] + "****" + value[^4..]
                        : "****";
                }
                else if (key.Contains("Key", StringComparison.OrdinalIgnoreCase))
                {
                    masked[key] = value.Length > 8
                        ? value[..4] + "****" + value[^4..]
                        : "****";
                }
                else
                {
                    // Secret olmayan alanlar duz gosterilir (BucketName, Region, FolderId vb.)
                    masked[key] = value;
                }
            }

            return masked;
        }
        catch
        {
            return new();
        }
    }

    private string BuildCredentialsJson(StorageConfigCreateDto dto)
    {
        object credentials = dto.ProviderTypeId switch
        {
            StorageProviders.Ids.AmazonS3 or StorageProviders.Ids.MinIO => new
            {
                AccessKey = dto.AccessKey ?? "",
                SecretKey = dto.SecretKey ?? "",
                BucketName = dto.BucketName ?? "",
                Region = dto.Region,
                Endpoint = dto.Endpoint,
                Prefix = dto.Prefix,
                UseSSL = dto.UseSSL
            },
            StorageProviders.Ids.GoogleDrive => new
            {
                ClientId = dto.GoogleClientId ?? "",
                ClientSecret = dto.GoogleClientSecret ?? "",
                RefreshToken = dto.GoogleRefreshToken ?? "",
                FolderId = dto.GoogleFolderId
            },
            StorageProviders.Ids.OneDrive => new
            {
                ClientId = dto.MsClientId ?? "",
                ClientSecret = dto.MsClientSecret ?? "",
                RefreshToken = dto.MsRefreshToken ?? "",
                TenantId = dto.MsTenantId,
                DriveId = dto.MsDriveId,
                FolderId = dto.MsFolderId
            },
            StorageProviders.Ids.YandexDisk => new
            {
                OAuthToken = dto.YandexOAuthToken ?? "",
                BasePath = dto.BasePath
            },
            _ => new { }
        };

        return JsonSerializer.Serialize(credentials);
    }

    private string MergeCredentialsJson(string existingJson, StorageConfigUpdateDto dto)
    {
        var existing = JsonSerializer.Deserialize<Dictionary<string, object>>(existingJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

        // S3/MinIO alanlari
        if (dto.AccessKey != null) existing["AccessKey"] = dto.AccessKey;
        if (dto.SecretKey != null) existing["SecretKey"] = dto.SecretKey;
        if (dto.BucketName != null) existing["BucketName"] = dto.BucketName;
        if (dto.Region != null) existing["Region"] = dto.Region;
        if (dto.Endpoint != null) existing["Endpoint"] = dto.Endpoint;
        if (dto.Prefix != null) existing["Prefix"] = dto.Prefix;
        if (dto.UseSSL.HasValue) existing["UseSSL"] = dto.UseSSL.Value;

        // Google
        if (dto.GoogleClientId != null) existing["ClientId"] = dto.GoogleClientId;
        if (dto.GoogleClientSecret != null) existing["ClientSecret"] = dto.GoogleClientSecret;
        if (dto.GoogleRefreshToken != null) existing["RefreshToken"] = dto.GoogleRefreshToken;
        if (dto.GoogleFolderId != null) existing["FolderId"] = dto.GoogleFolderId;

        // OneDrive
        if (dto.MsClientId != null) existing["ClientId"] = dto.MsClientId;
        if (dto.MsClientSecret != null) existing["ClientSecret"] = dto.MsClientSecret;
        if (dto.MsRefreshToken != null) existing["RefreshToken"] = dto.MsRefreshToken;
        if (dto.MsTenantId != null) existing["TenantId"] = dto.MsTenantId;
        if (dto.MsDriveId != null) existing["DriveId"] = dto.MsDriveId;
        if (dto.MsFolderId != null) existing["FolderId"] = dto.MsFolderId;

        // Yandex
        if (dto.YandexOAuthToken != null) existing["OAuthToken"] = dto.YandexOAuthToken;

        return JsonSerializer.Serialize(existing);
    }

    private static bool HasAnyCredentialField(StorageConfigUpdateDto dto)
    {
        return dto.AccessKey != null || dto.SecretKey != null || dto.BucketName != null ||
               dto.Region != null || dto.Endpoint != null || dto.Prefix != null || dto.UseSSL.HasValue ||
               dto.GoogleClientId != null || dto.GoogleClientSecret != null || dto.GoogleRefreshToken != null ||
               dto.GoogleFolderId != null ||
               dto.MsClientId != null || dto.MsClientSecret != null || dto.MsRefreshToken != null ||
               dto.MsTenantId != null || dto.MsDriveId != null || dto.MsFolderId != null ||
               dto.YandexOAuthToken != null;
    }

    private static List<string> GetRequiredFields(int providerId) => providerId switch
    {
        StorageProviders.Ids.AmazonS3 => new() { "AccessKey", "SecretKey", "BucketName", "Region" },
        StorageProviders.Ids.MinIO => new() { "Endpoint", "AccessKey", "SecretKey", "BucketName" },
        StorageProviders.Ids.GoogleDrive => new() { "GoogleClientId", "GoogleClientSecret", "GoogleRefreshToken" },
        StorageProviders.Ids.OneDrive => new() { "MsClientId", "MsClientSecret", "MsTenantId", "MsDriveId" },
        StorageProviders.Ids.YandexDisk => new() { "YandexOAuthToken" },
        _ => new()
    };
}
