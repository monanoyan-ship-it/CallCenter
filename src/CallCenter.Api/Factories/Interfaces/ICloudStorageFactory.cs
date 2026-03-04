using CallCenter.Shared.DTOs;
using CallCenter.Shared.Interfaces;

namespace CallCenter.Api.Factories.Interfaces;

public interface ICloudStorageFactory
{
    Task<StorageUploadResult> UploadRecordingAsync(int customerId, Stream fileStream, string fileName, CancellationToken ct = default);
    Task<Stream?> DownloadRecordingAsync(int customerId, string fileId, CancellationToken ct = default);
    Task<bool> DeleteRecordingAsync(int customerId, string fileId, CancellationToken ct = default);
    Task<string?> GetRecordingUrlAsync(int customerId, string fileId, CancellationToken ct = default);
    Task<RecordingUploadResultDto> UploadCallRecordingAsync(int customerId, Guid callUid, Stream fileStream, string fileName, CancellationToken ct = default);
    Task<RecordingDownloadUrlDto?> GetCallRecordingUrlAsync(int customerId, Guid callUid, CancellationToken ct = default);
    Task<bool> HasActiveConfigAsync(int customerId);
    Task<CloudConfigForClientDto?> GetConfigForClientAsync(int customerId);
    Task<List<StorageConfigListDto>> GetConfigsAsync(int? customerId = null);
    Task<StorageConfigDetailDto?> GetConfigByIdAsync(int configId);
    Task<(bool Success, int? Id, string? Error)> CreateConfigAsync(StorageConfigCreateDto dto);
    Task<(bool Success, string? Error)> UpdateConfigAsync(int configId, StorageConfigUpdateDto dto);
    Task<(bool Success, string? Error)> DeleteConfigAsync(int configId);
    Task<StorageTestResultDto> TestConnectionAsync(int configId, CancellationToken ct = default);
    List<StorageProviderInfoDto> GetAvailableProviders();
}
