using System.Text.Json;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Interfaces;

namespace CallCenter.Api.Services.CloudStorage;

/// <summary>
/// Musterinin storage config'ine gore dogru ICloudStorageProvider'i ureten fabrika.
/// Credential'lari AES-256 ile cozumler ve provider'a gecirir.
/// </summary>
public class CloudStorageFactory
{
    private readonly AesEncryptionService _encryption;
    private readonly IConfiguration _config;

    public CloudStorageFactory(AesEncryptionService encryption, IConfiguration config)
    {
        _encryption = encryption;
        _config = config;
    }

    /// <summary>
    /// Musterinin storage config'inden uygun provider olusturur.
    /// </summary>
    public ICloudStorageProvider Create(CustomerStorageConfig config)
    {
        var credentialsJson = _encryption.Decrypt(config.EncryptedCredentials);

        return config.ProviderTypeId switch
        {
            StorageProviders.Ids.AmazonS3 => CreateS3Provider(credentialsJson, isMinIO: false),
            StorageProviders.Ids.MinIO => CreateS3Provider(credentialsJson, isMinIO: true),
            StorageProviders.Ids.GoogleDrive => CreateGoogleDriveProvider(credentialsJson),
            StorageProviders.Ids.OneDrive => CreateOneDriveProvider(credentialsJson),
            StorageProviders.Ids.YandexDisk => CreateYandexDiskProvider(credentialsJson),
            StorageProviders.Ids.LocalDisk => throw new InvalidOperationException(
                "LocalDisk deposu sunucu tarafindan erisilemez. " +
                "Dosya islemleri istemci (Windows app) tarafindan yapilir."),
            _ => throw new NotSupportedException(
                $"StorageProvider {config.ProviderTypeId} desteklenmiyor. " +
                $"Desteklenen: GoogleDrive, OneDrive, YandexDisk, AmazonS3, MinIO, LocalDisk")
        };
    }

    private static S3StorageProvider CreateS3Provider(string json, bool isMinIO)
    {
        var creds = JsonSerializer.Deserialize<S3Credentials>(json, JsonOpts)
            ?? throw new InvalidOperationException("S3 credential bilgileri okunamadi");
        return new S3StorageProvider(creds, isMinIO);
    }

    private GoogleDriveStorageProvider CreateGoogleDriveProvider(string json)
    {
        var creds = JsonSerializer.Deserialize<GoogleDriveCredentials>(json, JsonOpts)
            ?? throw new InvalidOperationException("Google Drive credential bilgileri okunamadi");

        // Kolay modda ClientId/Secret bos olabilir — API'nin kendi credentials'ini kullan
        if (string.IsNullOrEmpty(creds.ClientId))
        {
            creds.ClientId = _config["GoogleDrive:ClientId"] ?? "";
            creds.ClientSecret = _config["GoogleDrive:ClientSecret"] ?? "";
        }

        return new GoogleDriveStorageProvider(creds);
    }

    private OneDriveStorageProvider CreateOneDriveProvider(string json)
    {
        var creds = JsonSerializer.Deserialize<OneDriveCredentials>(json, JsonOpts)
            ?? throw new InvalidOperationException("OneDrive credential bilgileri okunamadi");

        // Delegated modda ClientId/Secret appsettings'ten gelir
        if (creds.IsDelegated && string.IsNullOrEmpty(creds.ClientId))
        {
            creds.ClientId = _config["OneDrive:ClientId"] ?? "";
            creds.ClientSecret = _config["OneDrive:ClientSecret"] ?? "";
        }

        return new OneDriveStorageProvider(creds);
    }

    private static YandexDiskStorageProvider CreateYandexDiskProvider(string json)
    {
        var creds = JsonSerializer.Deserialize<YandexDiskCredentials>(json, JsonOpts)
            ?? throw new InvalidOperationException("Yandex Disk credential bilgileri okunamadi");
        return new YandexDiskStorageProvider(creds);
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
