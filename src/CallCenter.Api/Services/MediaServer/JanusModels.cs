using System.Text.Json.Serialization;

namespace CallCenter.Api.Services.MediaServer;

// ═══════════════════════════════════════════════════════════
// JANUS GATEWAY REST API MODELLERI
// ═══════════════════════════════════════════════════════════

/// <summary>Janus generic response.</summary>
public class JanusResponse
{
    [JsonPropertyName("janus")]
    public string Janus { get; set; } = string.Empty;

    [JsonPropertyName("transaction")]
    public string? Transaction { get; set; }

    [JsonPropertyName("session_id")]
    public long SessionId { get; set; }

    [JsonPropertyName("sender")]
    public long Sender { get; set; }

    [JsonPropertyName("error")]
    public JanusError? Error { get; set; }
}

public class JanusError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Session create response: { "janus": "success", "data": { "id": 12345 } }</summary>
public class JanusSessionResponse : JanusResponse
{
    [JsonPropertyName("data")]
    public JanusDataId? Data { get; set; }
}

/// <summary>Plugin attach response: { "janus": "success", "data": { "id": 67890 } }</summary>
public class JanusAttachResponse : JanusResponse
{
    [JsonPropertyName("data")]
    public JanusDataId? Data { get; set; }
}

public class JanusDataId
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}

/// <summary>Plugin message response with plugindata.</summary>
public class JanusPluginResponse : JanusResponse
{
    [JsonPropertyName("plugindata")]
    public JanusPluginData? PluginData { get; set; }
}

public class JanusPluginData
{
    [JsonPropertyName("plugin")]
    public string Plugin { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }
}

// ═══════════════════════════════════════════════════════════
// AUDIOBRIDGE PLUGIN MODELLERI
// ═══════════════════════════════════════════════════════════

/// <summary>AudioBridge room create request body.</summary>
public class AudioBridgeCreateRoomRequest
{
    [JsonPropertyName("request")]
    public string Request { get; set; } = "create";

    [JsonPropertyName("room")]
    public long Room { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("is_private")]
    public bool IsPrivate { get; set; } = true;

    [JsonPropertyName("sampling_rate")]
    public int SamplingRate { get; set; } = 16000;

    [JsonPropertyName("audiolevel_event")]
    public bool AudioLevelEvent { get; set; } = true;

    [JsonPropertyName("record")]
    public bool Record { get; set; }

    [JsonPropertyName("record_dir")]
    public string? RecordDir { get; set; }
}

/// <summary>AudioBridge join request.</summary>
public class AudioBridgeJoinRequest
{
    [JsonPropertyName("request")]
    public string Request { get; set; } = "join";

    [JsonPropertyName("room")]
    public long Room { get; set; }

    [JsonPropertyName("display")]
    public string? Display { get; set; }

    [JsonPropertyName("muted")]
    public bool Muted { get; set; }

    /// <summary>true = sadece dinleyici (ses gondermez)</summary>
    [JsonPropertyName("prebuffer")]
    public int Prebuffer { get; set; } = 6;
}

/// <summary>AudioBridge configure (mute/unmute, listen-only vs).</summary>
public class AudioBridgeConfigureRequest
{
    [JsonPropertyName("request")]
    public string Request { get; set; } = "configure";

    [JsonPropertyName("muted")]
    public bool? Muted { get; set; }

    [JsonPropertyName("prebuffer")]
    public int? Prebuffer { get; set; }
}

/// <summary>AudioBridge leave request.</summary>
public class AudioBridgeLeaveRequest
{
    [JsonPropertyName("request")]
    public string Request { get; set; } = "leave";
}

/// <summary>AudioBridge destroy room request.</summary>
public class AudioBridgeDestroyRequest
{
    [JsonPropertyName("request")]
    public string Request { get; set; } = "destroy";

    [JsonPropertyName("room")]
    public long Room { get; set; }
}

// ═══════════════════════════════════════════════════════════
// VIDEOROOM PLUGIN MODELLERI
// ═══════════════════════════════════════════════════════════

/// <summary>VideoRoom create request.</summary>
public class VideoRoomCreateRequest
{
    [JsonPropertyName("request")]
    public string Request { get; set; } = "create";

    [JsonPropertyName("room")]
    public long Room { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("is_private")]
    public bool IsPrivate { get; set; } = true;

    [JsonPropertyName("publishers")]
    public int Publishers { get; set; } = 6;

    [JsonPropertyName("bitrate")]
    public int Bitrate { get; set; } = 512000; // 512 kbps

    [JsonPropertyName("fir_freq")]
    public int FirFreq { get; set; } = 10; // Full Intra Request her 10 saniye

    [JsonPropertyName("videocodec")]
    public string VideoCodec { get; set; } = "vp8,h264,vp9";

    [JsonPropertyName("audiocodec")]
    public string AudioCodec { get; set; } = "opus";

    [JsonPropertyName("record")]
    public bool Record { get; set; }

    [JsonPropertyName("rec_dir")]
    public string? RecDir { get; set; }
}

/// <summary>VideoRoom join as publisher request.</summary>
public class VideoRoomJoinPublisherRequest
{
    [JsonPropertyName("request")]
    public string Request { get; set; } = "join";

    [JsonPropertyName("room")]
    public long Room { get; set; }

    [JsonPropertyName("ptype")]
    public string Ptype { get; set; } = "publisher";

    [JsonPropertyName("display")]
    public string? Display { get; set; }
}

/// <summary>VideoRoom join as subscriber request.</summary>
public class VideoRoomJoinSubscriberRequest
{
    [JsonPropertyName("request")]
    public string Request { get; set; } = "join";

    [JsonPropertyName("room")]
    public long Room { get; set; }

    [JsonPropertyName("ptype")]
    public string Ptype { get; set; } = "subscriber";

    [JsonPropertyName("feed")]
    public long Feed { get; set; } // Publisher ID to subscribe to
}

/// <summary>VideoRoom leave request.</summary>
public class VideoRoomLeaveRequest
{
    [JsonPropertyName("request")]
    public string Request { get; set; } = "leave";
}

/// <summary>VideoRoom destroy request.</summary>
public class VideoRoomDestroyRequest
{
    [JsonPropertyName("request")]
    public string Request { get; set; } = "destroy";

    [JsonPropertyName("room")]
    public long Room { get; set; }
}

// ═══════════════════════════════════════════════════════════
// JANUS SERVICE CONFIG
// ═══════════════════════════════════════════════════════════

/// <summary>Janus Gateway baglanti ayarlari (appsettings.json).</summary>
public class JanusConfig
{
    /// <summary>Janus REST API URL (ornek: http://localhost:8088/janus)</summary>
    public string ApiUrl { get; set; } = "http://localhost:8088/janus";

    /// <summary>Janus Admin API URL (opsiyonel)</summary>
    public string? AdminUrl { get; set; }

    /// <summary>Janus API secret (opsiyonel guvenlik)</summary>
    public string? ApiSecret { get; set; }

    /// <summary>Janus Admin secret</summary>
    public string? AdminSecret { get; set; }

    /// <summary>Kayit dosyalari dizini (Janus container icindeki yol)</summary>
    public string RecordDir { get; set; } = "/tmp/janus-recordings";
}
