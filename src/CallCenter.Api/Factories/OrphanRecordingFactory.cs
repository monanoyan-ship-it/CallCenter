using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class OrphanRecordingFactory : IOrphanRecordingFactory
{
    private readonly IOrphanRecordingEntityService _orphans;
    private readonly ICallRecordEntityService _calls;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly ICloudStorageFactory _cloudStorage;
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;
    private readonly ILogger<OrphanRecordingFactory> _logger;

    public OrphanRecordingFactory(
        IOrphanRecordingEntityService orphans,
        ICallRecordEntityService calls,
        ICustomerPersonnelEntityService personnel,
        ICloudStorageFactory cloudStorage,
        IUnitOfWork uow,
        IConfiguration config,
        ILogger<OrphanRecordingFactory> logger)
    {
        _orphans = orphans;
        _calls = calls;
        _personnel = personnel;
        _cloudStorage = cloudStorage;
        _uow = uow;
        _config = config;
        _logger = logger;
    }

    public async Task<OrphanRecordingDto> RegisterAsync(int customerId, RegisterOrphanRecordingRequest req)
    {
        // Idempotent: ayni musteri+CloudFileId varsa mevcut kaydi dondur (WinApp retry edebilir).
        OrphanRecording? entity = null;
        if (!string.IsNullOrWhiteSpace(req.CloudFileId))
            entity = await _orphans.GetByCloudFileIdAsync(customerId, req.CloudFileId);

        if (entity == null)
        {
            entity = new OrphanRecording
            {
                CustomerId = customerId,
                FileName = req.FileName,
                RecordedAt = req.RecordedAt,
                FileSize = req.FileSize,
                FileHash = req.FileHash,
                IsEncrypted = req.IsEncrypted,
                CloudFileId = req.CloudFileId,
                CloudFileName = req.CloudFileName ?? req.FileName,
                PlatformFileId = req.PlatformFileId,
                AgentName = req.AgentName,
                MachineId = req.MachineId,
                RetentionDate = req.RetentionDate ?? DateTime.UtcNow.AddYears(10),
                CreatedAt = DateTime.UtcNow
            };
            _orphans.Add(entity);
            await _uow.SaveChangesAsync();
        }
        else if (!string.IsNullOrWhiteSpace(req.PlatformFileId) && string.IsNullOrWhiteSpace(entity.PlatformFileId))
        {
            // Ikinci hedef (platform) sonradan tamamlandi.
            entity.PlatformFileId = req.PlatformFileId;
            _orphans.Update(entity);
            await _uow.SaveChangesAsync();
        }

        return ToDto(entity);
    }

    public async Task<List<OrphanRecordingDto>> ListUnresolvedAsync(int customerId)
    {
        var list = await _orphans.GetAllQueryable()
            .Where(o => o.CustomerId == customerId && !o.IsResolved)
            .OrderByDescending(o => o.RecordedAt ?? o.CreatedAt)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    public async Task<(Stream? AudioStream, string ContentType)?> StreamAsync(Guid orphanUid, CurrentUserInfo currentUser)
    {
        var orphan = await _orphans.GetByUidAsync(orphanUid);
        if (orphan == null) return null;

        if (!CanManage(orphan, currentUser)) return null;

        if (string.IsNullOrEmpty(orphan.CloudFileId))
            return (null, "Kayit bulut'ta bulunamadi (CloudFileId bos)");

        Stream? stream;
        try
        {
            stream = await _cloudStorage.DownloadRecordingAsync(orphan.CustomerId, orphan.CloudFileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Orphan] Cloud download HATA: CustomerId={CustomerId}, FileId={FileId}",
                orphan.CustomerId, orphan.CloudFileId);
            return (null, "Bulut depolamadan indirme hatasi");
        }

        if (stream == null)
            return (null, "Dosya bulut deposunda bulunamadi");

        if (orphan.IsEncrypted)
        {
            var keyString = _config["Encryption:Key"];
            if (string.IsNullOrEmpty(keyString))
                throw new InvalidOperationException("Encryption:Key yapilandirilmamis.");

            var key = FileEncryptionService.DeriveKey(keyString);
            var decrypted = new MemoryStream();
            try
            {
                await FileEncryptionService.DecryptStreamToStreamAsync(stream, decrypted, key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Orphan] Decrypt HATA: OrphanUid={OrphanUid}", orphanUid);
                await stream.DisposeAsync();
                return null;
            }
            await stream.DisposeAsync();
            decrypted.Position = 0;
            stream = decrypted;
        }

        var contentType = orphan.CloudFileName?.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) == true
            ? "audio/mpeg"
            : "audio/wav";

        return (stream, contentType);
    }

    public async Task<bool> MatchAsync(Guid orphanUid, Guid callUid, CurrentUserInfo currentUser)
    {
        var orphan = await _orphans.GetByUidAsync(orphanUid);
        if (orphan == null || orphan.IsResolved) return false;
        if (!CanManage(orphan, currentUser)) return false;

        var call = await _calls.GetByUidAsync(callUid);
        if (call == null) return false;

        // Hedef cagri ayni musteriye ait olmali.
        var callCustomerId = await GetCallCustomerIdAsync(call);
        if (callCustomerId == null || callCustomerId != orphan.CustomerId)
        {
            _logger.LogWarning("[Orphan] Esleme reddedildi - farkli musteri: OrphanCustomer={OC}, CallCustomer={CC}",
                orphan.CustomerId, callCustomerId);
            return false;
        }

        // Bulut referanslarini cagriya kopyala.
        call.CloudFileId = orphan.CloudFileId;
        call.CloudFileName = orphan.CloudFileName;
        if (string.IsNullOrEmpty(call.PlatformFileId))
            call.PlatformFileId = orphan.PlatformFileId;
        call.IsRecordingEncrypted = orphan.IsEncrypted;
        call.RecordingFileSize ??= orphan.FileSize;
        call.RecordingFileHash ??= orphan.FileHash;
        call.CloudUploadedAt ??= DateTime.UtcNow;
        call.RecordingRetentionDate ??= orphan.RetentionDate;
        _calls.Update(call);

        orphan.IsResolved = true;
        orphan.ResolvedCallRecordId = call.Id;
        orphan.ResolvedAt = DateTime.UtcNow;
        orphan.ResolvedByUserId = currentUser.UserId;
        _orphans.Update(orphan);

        await _uow.SaveChangesAsync();
        _logger.LogInformation("[Orphan] Eslendi: OrphanUid={OrphanUid} -> CallId={CallId}", orphanUid, call.Id);
        return true;
    }

    private static bool CanManage(OrphanRecording orphan, CurrentUserInfo user)
    {
        if (user.IsAdmin) return true;
        return user.IsCustomerAdmin && user.CustomerId != null && user.CustomerId == orphan.CustomerId;
    }

    private async Task<int?> GetCallCustomerIdAsync(CallRecord call)
    {
        if (call.QueueId != null)
        {
            var customerId = await _calls.GetAllQueryable()
                .Where(c => c.Id == call.Id && c.Queue != null)
                .Select(c => c.Queue!.CustomerId)
                .FirstOrDefaultAsync();
            if (customerId != 0) return customerId;
        }

        if (call.AgentId != null)
        {
            var personnel = await _personnel.GetByUserIdAsync(call.AgentId.Value);
            return personnel?.CustomerId;
        }

        return null;
    }

    private static OrphanRecordingDto ToDto(OrphanRecording o) => new()
    {
        Uid = o.Uid,
        FileName = o.FileName,
        RecordedAt = o.RecordedAt,
        FileSize = o.FileSize,
        IsEncrypted = o.IsEncrypted,
        HasCloudFile = !string.IsNullOrEmpty(o.CloudFileId),
        AgentName = o.AgentName,
        MachineId = o.MachineId,
        CreatedAt = o.CreatedAt
    };
}
