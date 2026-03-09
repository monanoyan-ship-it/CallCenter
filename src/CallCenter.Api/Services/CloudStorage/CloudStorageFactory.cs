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

    public CloudStorageFactory(AesEncryptionService encryption)
    {
        _encryption = encryption;
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

    private static GoogleDriveStorageProvider CreateGoogleDriveProvider(string json)
    {
        var creds = JsonSerializer.Deserialize<GoogleDriveCredentials>(json, JsonOpts)
            ?? throw new InvalidOperationException("Google Drive credential bilgileri okunamadi");
        return new GoogleDriveStorageProvider(creds);
    }

    private static OneDriveStorageProvider CreateOneDriveProvider(string json)
    {
        var creds = JsonSerializer.Deserialize<OneDriveCredentials>(json, JsonOpts)
            ?? throw new InvalidOperationException("OneDrive credential bilgileri okunamadi");
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
