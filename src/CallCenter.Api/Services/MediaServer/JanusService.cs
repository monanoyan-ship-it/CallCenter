using System.Text.Json;
using Microsoft.Extensions.Options;

namespace CallCenter.Api.Services.MediaServer;

/// <summary>
/// Janus Gateway REST API client.
/// Session yonetimi, AudioBridge (ses konferans), Monitoring (dinle/fisilda/katil).
/// </summary>
public class JanusService : IJanusService
{
    private readonly HttpClient _http;
    private readonly JanusConfig _config;
    private readonly ILogger<JanusService> _logger;
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public JanusService(HttpClient http, IOptions<JanusConfig> config, ILogger<JanusService> logger)
    {
        _http = http;
        _config = config.Value;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════
    // HEALTH CHECK
    // ═══════════════════════════════════════════════════

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await PostAsync(_config.ApiUrl, new { janus = "info" }, ct);
            return resp?.Janus == "server_info";
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Janus Gateway erisilemez: {Message}", ex.Message);
            return false;
        }
    }

    // ═══════════════════════════════════════════════════
    // SESSION YONETIMI
    // ═══════════════════════════════════════════════════

    public async Task<long?> CreateSessionAsync(CancellationToken ct = default)
    {
        var resp = await PostAsync<JanusSessionResponse>(_config.ApiUrl, new
        {
            janus = "create",
            transaction = NewTxn()
        }, ct);

        if (resp?.Janus != "success" || resp.Data == null)
        {
            _logger.LogError("Janus session olusturulamadi: {Error}", resp?.Error?.Reason);
            return null;
        }

        _logger.LogInformation("Janus session olusturuldu: {SessionId}", resp.Data.Id);
        return resp.Data.Id;
    }

    public async Task<long?> AttachPluginAsync(long sessionId, string pluginName, CancellationToken ct = default)
    {
        var url = $"{_config.ApiUrl}/{sessionId}";
        var resp = await PostAsync<JanusAttachResponse>(url, new
        {
            janus = "attach",
            plugin = pluginName,
            transaction = NewTxn()
        }, ct);

        if (resp?.Janus != "success" || resp.Data == null)
        {
            _logger.LogError("Janus plugin attach basarisiz ({Plugin}): {Error}", pluginName, resp?.Error?.Reason);
            return null;
        }

        _logger.LogInformation("Janus plugin attached: {Plugin} → handle {HandleId}", pluginName, resp.Data.Id);
        return resp.Data.Id;
    }

    public async Task<bool> DestroySessionAsync(long sessionId, CancellationToken ct = default)
    {
        var url = $"{_config.ApiUrl}/{sessionId}";
        var resp = await PostAsync(url, new { janus = "destroy", transaction = NewTxn() }, ct);
        return resp?.Janus == "success";
    }

    // ═══════════════════════════════════════════════════
    // AUDIOBRIDGE (SES KONFERANS)
    // ═══════════════════════════════════════════════════

    public async Task<bool> CreateAudioBridgeRoomAsync(long sessionId, long handleId, long roomId, string? description, bool record, CancellationToken ct = default)
    {
        var body = new AudioBridgeCreateRoomRequest
        {
            Room = roomId,
            Description = description ?? $"Room-{roomId}",
            Record = record,
            RecordDir = record ? _config.RecordDir : null
        };

        var resp = await SendMessageAsync(sessionId, handleId, body, ct);
        var success = IsPluginSuccess(resp, "created");

        if (success)
            _logger.LogInformation("AudioBridge odasi olusturuldu: {RoomId}", roomId);
        else
            _logger.LogError("AudioBridge oda olusturma basarisiz: {RoomId}", roomId);

        return success;
    }

    public async Task<bool> DestroyAudioBridgeRoomAsync(long sessionId, long handleId, long roomId, CancellationToken ct = default)
    {
        var body = new AudioBridgeDestroyRequest { Room = roomId };
        var resp = await SendMessageAsync(sessionId, handleId, body, ct);
        return IsPluginSuccess(resp, "destroyed");
    }

    public async Task<bool> JoinAudioBridgeRoomAsync(long sessionId, long handleId, long roomId, string? displayName, bool muted, CancellationToken ct = default)
    {
        var body = new AudioBridgeJoinRequest
        {
            Room = roomId,
            Display = displayName,
            Muted = muted
        };

        var resp = await SendMessageAsync(sessionId, handleId, body, ct);
        return IsPluginSuccess(resp, "joined");
    }

    public async Task<bool> LeaveAudioBridgeRoomAsync(long sessionId, long handleId, CancellationToken ct = default)
    {
        var body = new AudioBridgeLeaveRequest();
        var resp = await SendMessageAsync(sessionId, handleId, body, ct);
        return IsPluginSuccess(resp, "left");
    }

    public async Task<bool> ConfigureParticipantAsync(long sessionId, long handleId, bool? muted, CancellationToken ct = default)
    {
        var body = new AudioBridgeConfigureRequest { Muted = muted };
        var resp = await SendMessageAsync(sessionId, handleId, body, ct);
        return IsPluginSuccess(resp, "ok");
    }

    // ═══════════════════════════════════════════════════
    // MONITORING
    // ═══════════════════════════════════════════════════

    public async Task<bool> JoinAsListenerAsync(long sessionId, long handleId, long roomId, string? displayName, CancellationToken ct = default)
    {
        // Listen-only: muted=true, ses gondermez sadece dinler
        return await JoinAudioBridgeRoomAsync(sessionId, handleId, roomId, displayName ?? "Supervisor", muted: true, ct);
    }

    public async Task<bool> SwitchToWhisperAsync(long sessionId, long handleId, CancellationToken ct = default)
    {
        // Whisper: Supervisor unmute (sesi agent'a gider)
        // Not: Gercek whisper (sadece agent'a ses) icin Janus forward plugin veya
        // room bazli izolasyon gerekir. Basit implementasyon: supervisor unmute.
        _logger.LogInformation("Monitoring modu: Whisper (supervisor unmute)");
        return await ConfigureParticipantAsync(sessionId, handleId, muted: false, ct);
    }

    public async Task<bool> SwitchToBargeInAsync(long sessionId, long handleId, CancellationToken ct = default)
    {
        // Barge-In: Supervisor tam katilimci olarak unmute
        _logger.LogInformation("Monitoring modu: Barge-In (supervisor tam katilimci)");
        return await ConfigureParticipantAsync(sessionId, handleId, muted: false, ct);
    }

    // ═══════════════════════════════════════════════════
    // VIDEOROOM (VIDEO SFU KONFERANS)
    // ═══════════════════════════════════════════════════

    public async Task<bool> CreateVideoRoomAsync(long sessionId, long handleId, long roomId, string? description, int maxPublishers = 6, CancellationToken ct = default)
    {
        var body = new VideoRoomCreateRequest
        {
            Room = roomId,
            Description = description ?? $"VideoRoom-{roomId}",
            Publishers = maxPublishers,
            Record = false
        };

        var resp = await SendMessageAsync(sessionId, handleId, body, ct);
        var success = IsPluginSuccess(resp, "created");

        if (success)
            _logger.LogInformation("Janus VideoRoom odasi olusturuldu: {RoomId} (max {MaxPublishers} publisher)", roomId, maxPublishers);
        else
            _logger.LogError("Janus VideoRoom oda olusturma basarisiz: {RoomId}", roomId);

        return success;
    }

    public async Task<bool> DestroyVideoRoomAsync(long sessionId, long handleId, long roomId, CancellationToken ct = default)
    {
        var body = new VideoRoomDestroyRequest { Room = roomId };
        var resp = await SendMessageAsync(sessionId, handleId, body, ct);
        return IsPluginSuccess(resp, "destroyed");
    }

    public async Task<bool> JoinVideoRoomAsPublisherAsync(long sessionId, long handleId, long roomId, string? displayName, CancellationToken ct = default)
    {
        var body = new VideoRoomJoinPublisherRequest
        {
            Room = roomId,
            Display = displayName
        };

        var resp = await SendMessageAsync(sessionId, handleId, body, ct);
        // VideoRoom join response: { "videoroom": "joined" }
        if (resp?.PluginData?.Data?.TryGetValue("videoroom", out var val) == true)
            return val?.ToString() == "joined";

        return false;
    }

    public async Task<bool> JoinVideoRoomAsSubscriberAsync(long sessionId, long handleId, long roomId, long publisherId, CancellationToken ct = default)
    {
        var body = new VideoRoomJoinSubscriberRequest
        {
            Room = roomId,
            Feed = publisherId
        };

        var resp = await SendMessageAsync(sessionId, handleId, body, ct);
        if (resp?.PluginData?.Data?.TryGetValue("videoroom", out var val) == true)
            return val?.ToString() == "attached";

        return false;
    }

    public async Task<bool> LeaveVideoRoomAsync(long sessionId, long handleId, CancellationToken ct = default)
    {
        var body = new VideoRoomLeaveRequest();
        var resp = await SendMessageAsync(sessionId, handleId, body, ct);
        return IsPluginSuccess(resp, "left") ||
               (resp?.PluginData?.Data?.TryGetValue("videoroom", out var val) == true && val?.ToString() == "left");
    }

    // ═══════════════════════════════════════════════════
    // GENEL MESAJ GONDERME
    // ═══════════════════════════════════════════════════

    public async Task<JanusPluginResponse?> SendMessageAsync(long sessionId, long handleId, object body, CancellationToken ct = default)
    {
        var url = $"{_config.ApiUrl}/{sessionId}/{handleId}";
        var payload = new
        {
            janus = "message",
            transaction = NewTxn(),
            body
        };

        return await PostAsync<JanusPluginResponse>(url, payload, ct);
    }

    // ═══════════════════════════════════════════════════
    // HELPER
    // ═══════════════════════════════════════════════════

    private async Task<JanusResponse?> PostAsync(string url, object payload, CancellationToken ct)
    {
        return await PostAsync<JanusResponse>(url, payload, ct);
    }

    private async Task<T?> PostAsync<T>(string url, object payload, CancellationToken ct) where T : JanusResponse
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, _jsonOpts);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<T>(responseJson, _jsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Janus API hatasi: {Url}", url);
            return null;
        }
    }

    private static bool IsPluginSuccess(JanusPluginResponse? resp, string expectedResult)
    {
        if (resp?.PluginData?.Data == null) return false;

        // AudioBridge response: { "audiobridge": "created" } veya { "audiobridge": "joined" }
        if (resp.PluginData.Data.TryGetValue("audiobridge", out var val))
        {
            return val?.ToString() == expectedResult;
        }

        // Generic: { "result": "ok" }
        if (resp.PluginData.Data.TryGetValue("result", out var res))
        {
            return res?.ToString() == expectedResult;
        }

        return false;
    }

    private static string NewTxn() => Guid.NewGuid().ToString("N")[..12];
}
