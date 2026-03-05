namespace CallCenter.PbxService.Services;

public interface ICallSessionManager
{
    CallSession? GetSession(string callId);
    CallSession CreateSession(string callId, string callerUri, string calleeUri);
    bool RemoveSession(string callId);
    IReadOnlyCollection<CallSession> GetActiveSessions();
    int ActiveCallCount { get; }
}

public class CallSession
{
    public string CallId { get; init; } = string.Empty;
    public string CallerUri { get; init; } = string.Empty;
    public string CalleeUri { get; init; } = string.Empty;
    public CallSessionState State { get; set; } = CallSessionState.Incoming;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? AssignedAgentId { get; set; }
    public int? QueueId { get; set; }
    public int? CallRecordId { get; set; }
}

public enum CallSessionState
{
    Incoming,       // INVITE alindi, henuz islenmedi
    IvrPlaying,     // IVR menusunde
    Queued,         // Kuyrukta bekliyor
    Ringing,        // Agent'a INVITE gonderildi, caliyor
    Connected,      // Agent ile gorusme aktif
    OnHold,         // Beklemede
    Transferring,   // Transfer ediliyor
    Completed,      // Normal bitti (BYE)
    Failed          // Hata ile bitti
}
