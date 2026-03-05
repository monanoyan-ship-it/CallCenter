using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IRecordingPlaybackFactory
{
    Task<RecordingInfoDto?> GetRecordingInfoAsync(Guid callUid, CurrentUserInfo currentUser);
    Task<(Stream? AudioStream, string ContentType)?> StreamRecordingAsync(Guid callUid, CurrentUserInfo currentUser, string? ipAddress, string? userAgent);
    Task LogStreamEndedAsync(Guid callUid, CurrentUserInfo currentUser, string? ipAddress, string? userAgent);
}
