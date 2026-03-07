namespace CallCenter.Shared.Enums;

public static class AudioCodecs
{
    public static readonly TypeItem PCMU = new(1, "PCMU", "AudioCodec.PCMU", "G.711 µ-law (8kHz, 64kbps)", "bi-soundwave", "bg-secondary", 1);
    public static readonly TypeItem PCMA = new(2, "PCMA", "AudioCodec.PCMA", "G.711 A-law (8kHz, 64kbps)", "bi-soundwave", "bg-secondary", 2);
    public static readonly TypeItem G722 = new(3, "G722", "AudioCodec.G722", "G.722 Wideband (16kHz, 64kbps)", "bi-soundwave", "bg-info", 3);
    public static readonly TypeItem Opus = new(4, "Opus", "AudioCodec.Opus", "Opus (8-48kHz, 6-510kbps, adaptif)", "bi-soundwave", "bg-success", 4, isDefault: true);
    public static readonly TypeItem G726 = new(5, "G726", "AudioCodec.G726", "G.726 ADPCM (8kHz, 32kbps)", "bi-soundwave", "bg-secondary", 5);
    public static readonly TypeItem Speex = new(6, "Speex", "AudioCodec.Speex", "Speex (8-32kHz, degisken)", "bi-soundwave", "bg-warning text-dark", 6);
    public static readonly TypeItem ILBC = new(7, "iLBC", "AudioCodec.iLBC", "iLBC (8kHz, 13.3/15.2kbps)", "bi-soundwave", "bg-dark", 7);

    public static IEnumerable<TypeItem> All => new[] { PCMU, PCMA, G722, Opus, G726, Speex, ILBC };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    /// <summary>Varsayilan codec oncelik sirasi (en yuksek kalite once)</summary>
    public static IEnumerable<TypeItem> DefaultPriority => new[] { Opus, G722, PCMU, PCMA };

    /// <summary>Web (WebRTC) tarafinda desteklenen codec'ler</summary>
    public static IEnumerable<TypeItem> WebSupported => new[] { Opus, G722, PCMU, PCMA };

    /// <summary>Windows (SIPSorcery) tarafinda desteklenen codec'ler</summary>
    public static IEnumerable<TypeItem> WindowsSupported => All;

    public static class Ids
    {
        public const int PCMU = 1;
        public const int PCMA = 2;
        public const int G722 = 3;
        public const int Opus = 4;
        public const int G726 = 5;
        public const int Speex = 6;
        public const int ILBC = 7;
    }
}

public static class VideoCodecs
{
    public static readonly TypeItem VP8 = new(1, "VP8", "VideoCodec.VP8", "VP8 (WebRTC varsayilan, 720p)", "bi-camera-video", "bg-success", 1, isDefault: true);
    public static readonly TypeItem H264 = new(2, "H264", "VideoCodec.H264", "H.264/AVC (yuksek uyumluluk)", "bi-camera-video-fill", "bg-primary", 2);
    public static readonly TypeItem VP9 = new(3, "VP9", "VideoCodec.VP9", "VP9 (verimli, yuksek kalite)", "bi-camera-video", "bg-info", 3);

    public static IEnumerable<TypeItem> All => new[] { VP8, H264, VP9 };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    /// <summary>Web (WebRTC) tarafinda desteklenen video codec'ler</summary>
    public static IEnumerable<TypeItem> WebSupported => All;

    /// <summary>Windows (SIPSorcery) tarafinda desteklenen video codec'ler</summary>
    public static IEnumerable<TypeItem> WindowsSupported => new[] { VP8, H264 };

    public static class Ids
    {
        public const int VP8 = 1;
        public const int H264 = 2;
        public const int VP9 = 3;
    }
}
