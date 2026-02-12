using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Interfaces;

public interface ICallService
{
    Task<object> GetHistoryAsync(int userId, int page, int pageSize);
    Task<List<CallNotification>> GetActiveAsync(int userId);
    Task<(int Id, Guid Uid)> StartCallAsync(int userId, StartCallRequest request);
    Task<(bool Success, string? Error)> HoldCallAsync(int callId);
    Task<(bool Success, string? Error)> EndCallAsync(int callId, int userId);
    Task<(bool Success, string? Error)> AnswerCallAsync(int callId);
    Task<List<CallNotification>> GetQueuedAsync(int? customerId);
    Task<object> IncomingCallAsync(IncomingCallRequest request);

    /// <summary>
    /// Windows uygulamasindan gelen senkronizasyon push'u.
    /// Uid bazli idempotent: ayni Uid varsa gunceller, yoksa yeni olusturur.
    /// </summary>
    Task<CallSyncPushResponse> SyncPushAsync(int userId, CallSyncPushRequest request);
}
