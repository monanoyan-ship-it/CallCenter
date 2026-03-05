using System.Collections.Concurrent;

namespace CallCenter.PbxService.Services;

public class CallSessionManager : ICallSessionManager
{
    private readonly ConcurrentDictionary<string, CallSession> _sessions = new();
    private readonly ILogger<CallSessionManager> _logger;

    public int ActiveCallCount => _sessions.Count;

    public CallSessionManager(ILogger<CallSessionManager> logger)
    {
        _logger = logger;
    }

    public CallSession? GetSession(string callId)
    {
        _sessions.TryGetValue(callId, out var session);
        return session;
    }

    public CallSession CreateSession(string callId, string callerUri, string calleeUri)
    {
        var session = new CallSession
        {
            CallId = callId,
            CallerUri = callerUri,
            CalleeUri = calleeUri
        };

        if (!_sessions.TryAdd(callId, session))
        {
            _logger.LogWarning("CallId zaten mevcut: {CallId}", callId);
            return _sessions[callId];
        }

        _logger.LogInformation("Yeni cagri oturumu: {CallId} ({Caller} -> {Callee})",
            callId, callerUri, calleeUri);
        return session;
    }

    public bool RemoveSession(string callId)
    {
        if (_sessions.TryRemove(callId, out var session))
        {
            session.EndedAt = DateTime.UtcNow;
            session.State = CallSessionState.Completed;
            _logger.LogInformation("Cagri oturumu sonlandi: {CallId} (Sure: {Duration}s)",
                callId, session.AnsweredAt.HasValue
                    ? (int)(DateTime.UtcNow - session.AnsweredAt.Value).TotalSeconds
                    : 0);
            return true;
        }
        return false;
    }

    public IReadOnlyCollection<CallSession> GetActiveSessions()
    {
        return _sessions.Values.ToList().AsReadOnly();
    }
}
