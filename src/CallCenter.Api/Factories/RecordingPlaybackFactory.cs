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
    private readonly ILogger<RecordingPlaybackFactory> _logger;

    public RecordingPlaybackFactory(
        ICallRecordEntityService calls,
        IRecordingAccessLogEntityService accessLogs,
        ICustomerPersonnelEntityService personnel,
        ICloudStorageFactory cloudStorage,
        IUnitOfWork uow,
        IConfiguration config,
        ILogger<RecordingPlaybackFactory> logger)
    {
        _calls = calls;
        _accessLogs = accessLogs;
        _personnel = personnel;
        _cloudStorage = cloudStorage;
        _uow = uow;
        _config = config;
        _logger = logger;
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
        _logger.LogInformation("[Playback] StreamRecording istegi: CallUid={CallUid}, User={UserName}(Id={UserId}), IsAdmin={IsAdmin}, IsCustomerAdmin={IsCustomerAdmin}, IsSupervisor={IsSupervisor}, CustomerId={CustomerId}, PersonnelId={PersonnelId}",
            callUid, currentUser.UserName, currentUser.UserId, currentUser.IsAdmin, currentUser.IsCustomerAdmin, currentUser.IsSupervisor, currentUser.CustomerId, currentUser.PersonnelId);

        var call = await _calls.GetByUidAsync(callUid);
        if (call == null)
        {
            _logger.LogWarning("[Playback] Call bulunamadi: CallUid={CallUid}", callUid);
            return null;
        }

        _logger.LogInformation("[Playback] Call bulundu: CallId={CallId}, AgentId={AgentId}, QueueId={QueueId}, CloudFileId={CloudFileId}, PlatformFileId={PlatformFileId}, IsEncrypted={IsEncrypted}",
            call.Id, call.AgentId, call.QueueId, call.CloudFileId, call.PlatformFileId, call.IsRecordingEncrypted);

        if (!await CanAccessAsync(call, currentUser))
        {
            _logger.LogWarning("[Playback] Erisim REDDEDILDI: CallUid={CallUid}, User={UserName}", callUid, currentUser.UserName);
            await LogAccessAsync(call, currentUser, RecordingAccessActions.Ids.AccessDenied, ipAddress, userAgent, "Yetkisiz erisim");
            return null;
        }

        // Oncelik: musteri deposu (CloudFileId), yoksa platform deposu (PlatformFileId)
        var fileId = call.CloudFileId ?? call.PlatformFileId;
        if (string.IsNullOrEmpty(fileId))
        {
            _logger.LogWarning("[Playback] FileId bos: CallUid={CallUid}, CloudFileId={CloudFileId}, PlatformFileId={PlatformFileId}", callUid, call.CloudFileId, call.PlatformFileId);
            return null;
        }

        // Determine customer ID for cloud storage
        var customerId = call.Queue != null
            ? await _calls.GetAllQueryable()
                .Where(c => c.Id == call.Id)
                .Select(c => c.Queue!.CustomerId)
                .FirstOrDefaultAsync()
            : currentUser.CustomerId;

        if (customerId == null)
        {
            _logger.LogWarning("[Playback] CustomerId bulunamadi: CallUid={CallUid}, QueueNull={QueueNull}", callUid, call.Queue == null);
            return null;
        }

        _logger.LogInformation("[Playback] Download basliyor: CustomerId={CustomerId}, FileId={FileId}", customerId, fileId);

        // Log stream started
        await LogAccessAsync(call, currentUser, RecordingAccessActions.Ids.StreamStarted, ipAddress, userAgent);

        // Download from cloud (musteri deposu veya platform deposu)
        Stream? stream;
        try
        {
            stream = await _cloudStorage.DownloadRecordingAsync(customerId.Value, fileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Playback] Cloud download HATA: CustomerId={CustomerId}, FileId={FileId}", customerId, fileId);
            return null;
        }

        if (stream == null)
        {
            _logger.LogWarning("[Playback] Cloud download null dondu: CustomerId={CustomerId}, FileId={FileId}", customerId, fileId);
            return null;
        }

        _logger.LogInformation("[Playback] Download basarili: StreamLength={Length}", stream.CanSeek ? stream.Length : -1);

        // Decrypt if encrypted
        if (call.IsRecordingEncrypted)
        {
            var keyString = _config["Encryption:Key"];
            if (string.IsNullOrEmpty(keyString))
                throw new InvalidOperationException("Encryption:Key yapilandirilmamis.");

            var key = FileEncryptionService.DeriveKey(keyString);
            var decryptedStream = new MemoryStream();
            try
            {
                await FileEncryptionService.DecryptStreamToStreamAsync(stream, decryptedStream, key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Playback] Decrypt HATA: CallUid={CallUid}", callUid);
                await stream.DisposeAsync();
                return null;
            }
            await stream.DisposeAsync();
            decryptedStream.Position = 0;
            stream = decryptedStream;
            _logger.LogInformation("[Playback] Decrypt basarili: DecryptedLength={Length}", decryptedStream.Length);
        }

        var contentType = call.CloudFileName?.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) == true
            ? "audio/mpeg"
            : "audio/wav";

        _logger.LogInformation("[Playback] Stream hazirlandi: ContentType={ContentType}", contentType);
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
        _logger.LogInformation("[CanAccess] Kontrol basliyor: CallId={CallId}, User={UserName}, IsAdmin={IsAdmin}, IsCustomerAdmin={IsCustomerAdmin}, IsSupervisor={IsSupervisor}, CustomerId={CustomerId}",
            call.Id, currentUser.UserName, currentUser.IsAdmin, currentUser.IsCustomerAdmin, currentUser.IsSupervisor, currentUser.CustomerId);

        // Admin: tum kayitlar
        if (currentUser.IsAdmin)
        {
            _logger.LogInformation("[CanAccess] Admin erisimi ONAYLANDI");
            return true;
        }

        // CustomerAdmin: kendi sirketi
        if (currentUser.IsCustomerAdmin && currentUser.CustomerId != null)
        {
            var callCustomerId = await GetCallCustomerIdAsync(call);
            var result = callCustomerId == currentUser.CustomerId;
            _logger.LogInformation("[CanAccess] CustomerAdmin kontrolu: CallCustomerId={CallCustomerId}, UserCustomerId={UserCustomerId}, Sonuc={Result}",
                callCustomerId, currentUser.CustomerId, result);
            return result;
        }

        // Supervisor: ekibindeki agent'larin kayitlari
        if (currentUser.IsSupervisor && currentUser.PersonnelId != null && currentUser.CustomerId != null)
        {
            var callCustomerId = await GetCallCustomerIdAsync(call);
            if (callCustomerId != currentUser.CustomerId)
            {
                _logger.LogWarning("[CanAccess] Supervisor farkli firma: CallCustomerId={CallCustomerId}, UserCustomerId={UserCustomerId}", callCustomerId, currentUser.CustomerId);
                return false;
            }

            if (call.AgentId == null)
            {
                _logger.LogWarning("[CanAccess] Supervisor - call.AgentId null");
                return false;
            }

            var teamMemberIds = await _personnel.GetTeamMemberIdsAsync(currentUser.PersonnelId.Value, currentUser.CustomerId.Value);
            var hasAccess = teamMemberIds.Contains(call.AgentId.Value);
            _logger.LogInformation("[CanAccess] Supervisor ekip kontrolu: AgentId={AgentId}, TeamMembers=[{Members}], Sonuc={Result}",
                call.AgentId, string.Join(",", teamMemberIds), hasAccess);
            return hasAccess;
        }

        // Agent/Operator: erisim YOK
        _logger.LogWarning("[CanAccess] Erisim REDDEDILDI - hicbir role uymadi: User={UserName}, IsAdmin={IsAdmin}, IsCustomerAdmin={IsCustomerAdmin}, IsSupervisor={IsSupervisor}",
            currentUser.UserName, currentUser.IsAdmin, currentUser.IsCustomerAdmin, currentUser.IsSupervisor);
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
