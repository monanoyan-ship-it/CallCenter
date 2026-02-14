namespace CallCenter.Api.Services.MediaServer;

/// <summary>
/// Janus Gateway REST API client arayuzu.
/// AudioBridge (ses konferans) ve ileride VideoRoom (video SFU) islemleri.
/// </summary>
public interface IJanusService
{
    /// <summary>Janus Gateway'e erisilebilir mi?</summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    // ═══════════════════════════════════════════════════
    // SESSION YONETIMI
    // ═══════════════════════════════════════════════════

    /// <summary>Yeni Janus session olusturur.</summary>
    Task<long?> CreateSessionAsync(CancellationToken ct = default);

    /// <summary>Session'a plugin ekler (attach).</summary>
    Task<long?> AttachPluginAsync(long sessionId, string pluginName, CancellationToken ct = default);

    /// <summary>Session'i sonlandirir.</summary>
    Task<bool> DestroySessionAsync(long sessionId, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════
    // AUDIOBRIDGE (SES KONFERANS)
    // ═══════════════════════════════════════════════════

    /// <summary>AudioBridge odasi olusturur.</summary>
    Task<bool> CreateAudioBridgeRoomAsync(long sessionId, long handleId, long roomId, string? description, bool record = false, CancellationToken ct = default);

    /// <summary>AudioBridge odasini yok eder.</summary>
    Task<bool> DestroyAudioBridgeRoomAsync(long sessionId, long handleId, long roomId, CancellationToken ct = default);

    /// <summary>Katilimciyi AudioBridge odasina ekler.</summary>
    Task<bool> JoinAudioBridgeRoomAsync(long sessionId, long handleId, long roomId, string? displayName, bool muted = false, CancellationToken ct = default);

    /// <summary>Katilimciyi AudioBridge odasindan cikarir.</summary>
    Task<bool> LeaveAudioBridgeRoomAsync(long sessionId, long handleId, CancellationToken ct = default);

    /// <summary>Katilimcinin mute durumunu degistirir.</summary>
    Task<bool> ConfigureParticipantAsync(long sessionId, long handleId, bool? muted, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════
    // MONITORING (DINLEME/FISILDAMA/KATILMA)
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Supervisor'u AudioBridge odasina listen-only olarak ekler (Silent Monitor).
    /// Supervisor ses gondermez, sadece dinler.
    /// </summary>
    Task<bool> JoinAsListenerAsync(long sessionId, long handleId, long roomId, string? displayName, CancellationToken ct = default);

    /// <summary>
    /// Supervisor'u Whisper moduna gecirir: sadece agent'a ses gonder.
    /// Janus AudioBridge'de muted=false + ozel routing.
    /// Not: Gercek whisper icin Janus'un "forward" veya ozel plugin gerekir.
    /// Basit implementasyon: supervisor unmute + katilimcilara ses yayini.
    /// </summary>
    Task<bool> SwitchToWhisperAsync(long sessionId, long handleId, CancellationToken ct = default);

    /// <summary>
    /// Supervisor'u Barge-In moduna gecirir: tam katilimci olarak konferansa dahil.
    /// </summary>
    Task<bool> SwitchToBargeInAsync(long sessionId, long handleId, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════
    // VIDEOROOM (VIDEO SFU KONFERANS)
    // ═══════════════════════════════════════════════════

    /// <summary>VideoRoom odasi olusturur (SFU modu — max 6 katilimci).</summary>
    Task<bool> CreateVideoRoomAsync(long sessionId, long handleId, long roomId, string? description, int maxPublishers = 6, CancellationToken ct = default);

    /// <summary>VideoRoom odasini yok eder.</summary>
    Task<bool> DestroyVideoRoomAsync(long sessionId, long handleId, long roomId, CancellationToken ct = default);

    /// <summary>Publisher olarak VideoRoom'a katilir (ses + video gonderir).</summary>
    Task<bool> JoinVideoRoomAsPublisherAsync(long sessionId, long handleId, long roomId, string? displayName, CancellationToken ct = default);

    /// <summary>Subscriber olarak VideoRoom'a katilir (belirli publisher'i izler).</summary>
    Task<bool> JoinVideoRoomAsSubscriberAsync(long sessionId, long handleId, long roomId, long publisherId, CancellationToken ct = default);

    /// <summary>VideoRoom'dan ayrilir.</summary>
    Task<bool> LeaveVideoRoomAsync(long sessionId, long handleId, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════
    // GENEL
    // ═══════════════════════════════════════════════════

    /// <summary>Plugin'e mesaj gonderir (generic).</summary>
    Task<JanusPluginResponse?> SendMessageAsync(long sessionId, long handleId, object body, CancellationToken ct = default);
}
