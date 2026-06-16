using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IOrphanRecordingFactory
{
    /// <summary>WinApp eslesmemis kaydi buluta yukledikten sonra kaydeder (CloudFileId bazli idempotent).</summary>
    Task<OrphanRecordingDto> RegisterAsync(int customerId, RegisterOrphanRecordingRequest req);

    /// <summary>Musterinin cozulmemis (eslesmemis) kayit listesi.</summary>
    Task<List<OrphanRecordingDto>> ListUnresolvedAsync(int customerId);

    /// <summary>Orphan kaydi dinlemek icin bulut'tan indirir + (gerekiyorsa) decrypt eder.</summary>
    Task<(Stream? AudioStream, string ContentType)?> StreamAsync(Guid orphanUid, CurrentUserInfo currentUser);

    /// <summary>Orphan kaydi dogru CallRecord'a esler; bulut referanslarini cagriya kopyalar.</summary>
    Task<bool> MatchAsync(Guid orphanUid, Guid callUid, CurrentUserInfo currentUser);
}
