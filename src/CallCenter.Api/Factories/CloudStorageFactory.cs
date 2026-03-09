using System.Text.Json;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class CloudStorageFactory : ICloudStorageFactory
{
    private readonly IStorageConfigEntityService _configEs;
    private readonly ICallRecordEntityService _callEs;
    private readonly Services.CloudStorage.CloudStorageFactory _providerFactory;
    private readonly AesEncryptionService _encryption;
    private readonly IUnitOfWork _uow;

    public CloudStorageFactory(
        IStorageConfigEntityService configEs,
        ICallRecordEntityService callEs,
        Services.CloudStorage.CloudStorageFactory providerFactory,
        AesEncryptionService encryption,
        IUnitOfWork uow)
    {
        _configEs = configEs;
        _callEs = callEs;
        _providerFactory = providerFactory;
        _encryption = encryption;
        _uow = uow;
    }

    // FILE OPERATIONS

    public async Task<StorageUploadResult> UploadRecordingAsync(int customerId, Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var config = await GetDefaultConfigAsync(customerId);
        if (config == null)
            return new StorageUploadResult { Success = false, Error = "Musteri icin depolama yapilandirilmamis" };

        if (config.ProviderTypeId == StorageProviders.Ids.LocalDisk)
            return new StorageUploadResult { Success = false, Error = "LocalDisk deposu sunucu tarafindan erisilemez" };

        var provider = _providerFactory.Create(config);
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

        if (config.ProviderTypeId == StorageProviders.Ids.LocalDisk) return null;

        var provider = _providerFactory.Create(config);
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

        if (config.ProviderTypeId == StorageProviders.Ids.LocalDisk) return false;

        var provider = _providerFactory.Create(config);
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

        if (config.ProviderTypeId == StorageProviders.Ids.LocalDisk) return null;

        var provider = _providerFactory.Create(config);
        try
        {
            return await provider.GetDownloadUrlAsync(fileId, TimeSpan.FromMinutes(30), ct);
        }
        finally
        {
            if (provider is IDisposable d) d.Dispose();
        }
    }

    // CALL RECORDING UPLOAD/DOWNLOAD

    public async Task<RecordingUploadResultDto> UploadCallRecordingAsync(int customerId, Guid callUid, Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var callRecord = await _callEs.GetAllQueryable().FirstOrDefaultAsync(c => c.Uid == callUid, ct);
        if (callRecord == null)
            return new RecordingUploadResultDto { Success = false, Error = "Cagri kaydi bulunamadi" };

        var uploadResult = await UploadRecordingAsync(customerId, fileStream, fileName, ct);
        if (!uploadResult.Success)
            return new RecordingUploadResultDto { Success = false, Error = uploadResult.Error };

        var config = await GetDefaultConfigAsync(customerId);
        callRecord.CloudFileId = uploadResult.FileId;
        callRecord.CloudFileName = fileName;
        callRecord.CloudStorageConfigId = config?.Id;
        callRecord.CloudUploadedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(uploadResult.FileUrl))
            callRecord.RecordingUrl = uploadResult.FileUrl;

        await _uow.SaveChangesAsync(ct);

        return new RecordingUploadResultDto
        {
            Success = true,
            CloudFileId = uploadResult.FileId
        };
    }

    public async Task<RecordingDownloadUrlDto?> GetCallRecordingUrlAsync(int customerId, Guid callUid, CancellationToken ct = default)
    {
        var callRecord = await _callEs.GetAllQueryable().FirstOrDefaultAsync(c => c.Uid == callUid, ct);
        if (callRecord?.CloudFileId == null) return null;

        var url = await GetRecordingUrlAsync(customerId, callRecord.CloudFileId, ct);
        if (url == null) return null;

        return new RecordingDownloadUrlDto
        {
            Url = url,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };
    }

    public async Task<bool> HasActiveConfigAsync(int customerId)
    {
        return await _configEs.GetAllQueryable()
            .AnyAsync(c => c.CustomerId == customerId && c.IsActive);
    }

    public async Task<CloudConfigForClientDto?> GetConfigForClientAsync(int customerId)
    {
        var config = await GetDefaultConfigAsync(customerId);
        if (config == null) return null;

        try
        {
            var json = _encryption.Decrypt(config.EncryptedCredentials);
            var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            return new CloudConfigForClientDto
            {
                ProviderTypeId = config.ProviderTypeId,
                BasePath = config.BasePath,
                Credentials = credentials
            };
        }
        catch
        {
            return null;
        }
    }

    // CONFIG CRUD

    public async Task<List<StorageConfigListDto>> GetConfigsAsync(int? customerId = null)
    {
        var query = _configEs.GetAllQueryable()
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
        var config = await _configEs.GetAllQueryable()
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == configId);

        if (config == null) return null;

        var listDto = MapToListDto(config);
        return new StorageConfigDetailDto
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
    }

    public async Task<(bool Success, int? Id, string? Error)> CreateConfigAsync(StorageConfigCreateDto dto)
    {
        var provider = StorageProviders.GetById(dto.ProviderTypeId);
        if (provider == null)
            return (false, null, $"Gecersiz provider tipi: {dto.ProviderTypeId}");

        var credentialsJson = BuildCredentialsJson(dto);
        var encryptedCredentials = _encryption.Encrypt(credentialsJson);

        if (dto.IsDefault)
        {
            var existingDefaults = await _configEs.GetAllQueryable()
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

        _configEs.Add(config);
        await _uow.SaveChangesAsync();

        return (true, config.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateConfigAsync(int configId, StorageConfigUpdateDto dto)
    {
        var config = await _configEs.GetByIdAsync(configId);
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

        if (dto.IsDefault.HasValue && dto.IsDefault.Value && !config.IsDefault)
        {
            var existingDefaults = await _configEs.GetAllQueryable()
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

        if (HasAnyCredentialField(dto))
        {
            var existingJson = _encryption.Decrypt(config.EncryptedCredentials);
            var mergedJson = MergeCredentialsJson(existingJson, dto);
            config.EncryptedCredentials = _encryption.Encrypt(mergedJson);
        }

        config.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteConfigAsync(int configId)
    {
        var config = await _configEs.GetByIdAsync(configId);
        if (config == null)
            return (false, "Config bulunamadi");

        _configEs.Remove(config);
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    // CONNECTION TEST

    public async Task<StorageTestResultDto> TestConnectionAsync(int configId, CancellationToken ct = default)
    {
        var config = await _configEs.GetByIdAsync(configId);
        if (config == null)
            return new StorageTestResultDto { Success = false, Error = "Config bulunamadi", TestedAt = DateTime.UtcNow };

        // LocalDisk: sunucu tarafindan test edilemez, klasor erisimi istemci tarafinda dogrulanir
        if (config.ProviderTypeId == StorageProviders.Ids.LocalDisk)
        {
            config.LastTestedAt = DateTime.UtcNow;
            config.LastTestSuccess = true;
            config.LastTestError = null;
            await _uow.SaveChangesAsync();
            return new StorageTestResultDto { Success = true, TestedAt = DateTime.UtcNow };
        }

        var provider = _providerFactory.Create(config);
        try
        {
            var (success, error) = await provider.TestConnectionAsync(ct);

            config.LastTestedAt = DateTime.UtcNow;
            config.LastTestSuccess = success;
            config.LastTestError = error;
            await _uow.SaveChangesAsync();

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

    // PROVIDER INFO

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

    // HELPERS

    private async Task<CustomerStorageConfig?> GetDefaultConfigAsync(int customerId)
    {
        return await _configEs.GetAllQueryable()
            .Where(c => c.CustomerId == customerId && c.IsActive)
            .OrderByDescending(c => c.IsDefault)
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

            var masked = new Dictionary<string, string>();
            foreach (var (key, value) in dict)
            {
                if (string.IsNullOrEmpty(value))
                {
                    masked[key] = "";
                    continue;
                }

                if (key.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("Password", StringComparison.OrdinalIgnoreCase))
                {
                    masked[key] = value.Length > 8 ? value[..4] + "****" + value[^4..] : "****";
                }
                else if (key.Contains("Key", StringComparison.OrdinalIgnoreCase))
                {
                    masked[key] = value.Length > 8 ? value[..4] + "****" + value[^4..] : "****";
                }
                else
                {
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

        if (dto.AccessKey != null) existing["AccessKey"] = dto.AccessKey;
        if (dto.SecretKey != null) existing["SecretKey"] = dto.SecretKey;
        if (dto.BucketName != null) existing["BucketName"] = dto.BucketName;
        if (dto.Region != null) existing["Region"] = dto.Region;
        if (dto.Endpoint != null) existing["Endpoint"] = dto.Endpoint;
        if (dto.Prefix != null) existing["Prefix"] = dto.Prefix;
        if (dto.UseSSL.HasValue) existing["UseSSL"] = dto.UseSSL.Value;
        if (dto.GoogleClientId != null) existing["ClientId"] = dto.GoogleClientId;
        if (dto.GoogleClientSecret != null) existing["ClientSecret"] = dto.GoogleClientSecret;
        if (dto.GoogleRefreshToken != null) existing["RefreshToken"] = dto.GoogleRefreshToken;
        if (dto.GoogleFolderId != null) existing["FolderId"] = dto.GoogleFolderId;
        if (dto.MsClientId != null) existing["ClientId"] = dto.MsClientId;
        if (dto.MsClientSecret != null) existing["ClientSecret"] = dto.MsClientSecret;
        if (dto.MsRefreshToken != null) existing["RefreshToken"] = dto.MsRefreshToken;
        if (dto.MsTenantId != null) existing["TenantId"] = dto.MsTenantId;
        if (dto.MsDriveId != null) existing["DriveId"] = dto.MsDriveId;
        if (dto.MsFolderId != null) existing["FolderId"] = dto.MsFolderId;
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
