using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ICallFactory
{
    Task<object> GetHistoryAsync(int userId, int page, int pageSize);
    Task<List<CallNotification>> GetActiveAsync(int userId);
    Task<(int Id, Guid Uid)> StartCallAsync(int userId, StartCallRequest request);
    Task<(bool Success, string? Error)> HoldCallAsync(int callId);
    Task<(bool Success, string? Error)> EndCallAsync(int callId, int userId);
    Task<(bool Success, string? Error)> AnswerCallAsync(int callId);
    Task<List<CallNotification>> GetQueuedAsync(int? customerId);
    Task<object> IncomingCallAsync(IncomingCallRequest request);
    Task<CallSyncPushResponse> SyncPushAsync(int userId, CallSyncPushRequest request);
    Task<MyStatsResponse> GetMyStatsAsync(int userId);
    Task<(bool Success, string? Error)> UpdateNotesAsync(int callId, string? notes);

    Task<object> GetNumberHistoryAsync(int userId, string number);

    // ─── Callback (Geri Arama) Yonetimi ───
    Task<(bool Success, string? Error)> AssignCallbackAsync(int callId, int assignedToId, string? note, int adminUserId);
    Task<(bool Success, string? Error)> StartCallbackAsync(int callId, int userId);
    Task<(bool Success, string? Error)> CompleteCallbackAsync(int callId, int resultCallId);
    Task<(bool Success, string? Error)> RevertCallbackAsync(int callId);
    Task<List<CallCenter.Shared.DTOs.PendingCallbackDto>> GetPendingCallbacksAsync(int userId, int customerId);
}
