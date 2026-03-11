using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class RecordingPlaybackFactory : IRecordingPlaybackFactory
{
    private readonly ICallRecordEntityService _calls;
    private readonly IRecordingAccessLogEntityService _accessLogs;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly ICloudStorageFactory _cloudStorage;
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;

    public RecordingPlaybackFactory(
        ICallRecordEntityService calls,
        IRecordingAccessLogEntityService accessLogs,
        ICustomerPersonnelEntityService personnel,
        ICloudStorageFactory cloudStorage,
        IUnitOfWork uow,
        IConfiguration config)
    {
        _calls = calls;
        _accessLogs = accessLogs;
        _personnel = personnel;
        _cloudStorage = cloudStorage;
        _uow = uow;
        _config = config;
    }

    public async Task<RecordingInfoDto?> GetRecordingInfoAsync(Guid callUid, CurrentUserInfo currentUser)
    {
        var call = await _calls.GetByUidAsync(callUid);
        if (call == null) return null;

        if (!await CanAccessAsync(call, currentUser))
            return null;

        return new RecordingInfoDto
        {
            HasRecording = call.CloudFileId != null || call.PlatformFileId != null,
            IsEncrypted = call.IsRecordingEncrypted,
            FileSize = call.RecordingFileSize,
            DurationSeconds = call.DurationSeconds,
            CallerNumber = call.CallerNumber,
            CalleeNumber = call.CalleeNumber,
            CallDate = call.StartedAt
        };
    }

    public async Task<(Stream? AudioStream, string ContentType)?> StreamRecordingAsync(
        Guid callUid, CurrentUserInfo currentUser, string? ipAddress, string? userAgent)
    {
        var call = await _calls.GetByUidAsync(callUid);
        if (call == null) return null;

        if (!await CanAccessAsync(call, currentUser))
        {
            await LogAccessAsync(call, currentUser, RecordingAccessActions.Ids.AccessDenied, ipAddress, userAgent, "Yetkisiz erisim");
            return null;
        }

        // Oncelik: musteri deposu (CloudFileId), yoksa platform deposu (PlatformFileId)
        var fileId = call.CloudFileId ?? call.PlatformFileId;
        if (string.IsNullOrEmpty(fileId))
            return null;

        // Determine customer ID for cloud storage
        var customerId = call.Queue != null
            ? await _calls.GetAllQueryable()
                .Where(c => c.Id == call.Id)
                .Select(c => c.Queue!.CustomerId)
                .FirstOrDefaultAsync()
            : currentUser.CustomerId;

        if (customerId == null) return null;

        // Log stream started
        await LogAccessAsync(call, currentUser, RecordingAccessActions.Ids.StreamStarted, ipAddress, userAgent);

        // Download from cloud (musteri deposu veya platform deposu)
        var stream = await _cloudStorage.DownloadRecordingAsync(customerId.Value, fileId);
        if (stream == null) return null;

        // Decrypt if encrypted
        if (call.IsRecordingEncrypted)
        {
            var keyString = _config["Encryption:Key"];
            if (string.IsNullOrEmpty(keyString))
                throw new InvalidOperationException("Encryption:Key yapilandirilmamis.");

            var key = FileEncryptionService.DeriveKey(keyString);
            var decryptedStream = new MemoryStream();
            await FileEncryptionService.DecryptStreamToStreamAsync(stream, decryptedStream, key);
            await stream.DisposeAsync();
            decryptedStream.Position = 0;
            stream = decryptedStream;
        }

        var contentType = call.CloudFileName?.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) == true
            ? "audio/mpeg"
            : "audio/wav";

        return (stream, contentType);
    }

    public async Task LogStreamEndedAsync(Guid callUid, CurrentUserInfo currentUser, string? ipAddress, string? userAgent)
    {
        var call = await _calls.GetByUidAsync(callUid);
        if (call == null) return;

        await LogAccessAsync(call, currentUser, RecordingAccessActions.Ids.StreamEnded, ipAddress, userAgent);
    }

    private async Task<bool> CanAccessAsync(CallRecord call, CurrentUserInfo currentUser)
    {
        // Admin: tum kayitlar
        if (currentUser.IsAdmin) return true;

        // CustomerAdmin: kendi sirketi
        if (currentUser.IsCustomerAdmin && currentUser.CustomerId != null)
        {
            var callCustomerId = await GetCallCustomerIdAsync(call);
            return callCustomerId == currentUser.CustomerId;
        }

        // Supervisor: ekibindeki agent'larin kayitlari
        if (currentUser.IsSupervisor && currentUser.PersonnelId != null && currentUser.CustomerId != null)
        {
            var callCustomerId = await GetCallCustomerIdAsync(call);
            if (callCustomerId != currentUser.CustomerId) return false;

            if (call.AgentId == null) return false;

            var teamMemberIds = await _personnel.GetTeamMemberIdsAsync(currentUser.PersonnelId.Value, currentUser.CustomerId.Value);
            // TeamMemberIds returns user IDs of subordinates
            return teamMemberIds.Contains(call.AgentId.Value);
        }

        // Agent: erisim YOK
        return false;
    }

    private async Task<int?> GetCallCustomerIdAsync(CallRecord call)
    {
        // 1. Queue uzerinden musteri bul
        if (call.QueueId != null)
        {
            var customerId = await _calls.GetAllQueryable()
                .Where(c => c.Id == call.Id && c.Queue != null)
                .Select(c => c.Queue!.CustomerId)
                .FirstOrDefaultAsync();
            if (customerId != 0) return customerId;
        }

        // 2. Queue yoksa Agent uzerinden musteri bul (kuyruksuz aramalar)
        if (call.AgentId != null)
        {
            var personnel = await _personnel.GetByUserIdAsync(call.AgentId.Value);
            return personnel?.CustomerId;
        }

        return null;
    }

    private async Task LogAccessAsync(CallRecord call, CurrentUserInfo currentUser, int actionTypeId,
        string? ipAddress, string? userAgent, string? failureReason = null)
    {
        _accessLogs.Add(new RecordingAccessLog
        {
            CallRecordId = call.Id,
            AccessedByUserId = currentUser.UserId,
            AccessedByUserName = currentUser.UserName,
            CustomerId = currentUser.CustomerId,
            ActionTypeId = actionTypeId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            FailureReason = failureReason,
            AccessedAt = DateTime.UtcNow
        });
        await _uow.SaveChangesAsync();
    }
}
