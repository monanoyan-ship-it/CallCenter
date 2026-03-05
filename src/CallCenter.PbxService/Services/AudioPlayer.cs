namespace CallCenter.PbxService.Services;

/// <summary>
/// WAV dosyasini okuyup RTP frame'leri olarak gonderir.
/// Desteklenen format: PCM 8kHz 16-bit mono (standart telefon kalitesi).
/// 20ms frame = 160 sample = 320 byte PCM -> PCMU encode -> 160 byte.
/// </summary>
public class AudioPlayer : IAudioPlayer
{
    private readonly ILogger<AudioPlayer> _logger;
    private const int SampleRate = 8000;
    private const int FrameDurationMs = 20;
    private const int SamplesPerFrame = SampleRate * FrameDurationMs / 1000; // 160
    private const int BytesPerSample = 2; // 16-bit
    private const int PcmFrameSize = SamplesPerFrame * BytesPerSample; // 320 byte

    public AudioPlayer(ILogger<AudioPlayer> logger)
    {
        _logger = logger;
    }

    public async Task PlayFileAsync(string filePath, IRtpSender rtpSender, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Ses dosyasi bulunamadi: {Path}", filePath);
            return;
        }

        _logger.LogDebug("Ses oynatiliyor: {Path}", filePath);

        var pcmData = ReadWavPcmData(filePath);
        if (pcmData == null || pcmData.Length == 0)
        {
            _logger.LogWarning("Ses dosyasi okunamadi veya bos: {Path}", filePath);
            return;
        }

        await SendPcmFramesAsync(pcmData, rtpSender, cancellationToken);
    }

    public async Task PlayFilesAsync(IEnumerable<string> filePaths, IRtpSender rtpSender, CancellationToken cancellationToken)
    {
        foreach (var path in filePaths)
        {
            if (cancellationToken.IsCancellationRequested) break;
            await PlayFileAsync(path, rtpSender, cancellationToken);
        }
    }

    public async Task PlayLoopAsync(string filePath, IRtpSender rtpSender, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Loop ses dosyasi bulunamadi: {Path}", filePath);
            return;
        }

        var pcmData = ReadWavPcmData(filePath);
        if (pcmData == null || pcmData.Length == 0) return;

        _logger.LogDebug("Loop oynatiliyor: {Path}", filePath);

        while (!cancellationToken.IsCancellationRequested)
        {
            await SendPcmFramesAsync(pcmData, rtpSender, cancellationToken);
        }
    }

    private async Task SendPcmFramesAsync(byte[] pcmData, IRtpSender rtpSender, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset + PcmFrameSize <= pcmData.Length && !cancellationToken.IsCancellationRequested)
        {
            // 320 byte PCM -> 160 byte PCMU (mu-law)
            var pcmFrame = new Span<byte>(pcmData, offset, PcmFrameSize);
            var encodedFrame = EncodePcmToMuLaw(pcmFrame);

            await rtpSender.SendAudioFrameAsync(encodedFrame, FrameDurationMs);

            // Gercek zamanli oynatma icin bekle
            await Task.Delay(FrameDurationMs, cancellationToken);

            offset += PcmFrameSize;
        }
    }

    /// <summary>WAV dosyasindan ham PCM verisini oku (header'i atla)</summary>
    private byte[]? ReadWavPcmData(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            // RIFF header (12 byte)
            var riff = new string(reader.ReadChars(4));
            if (riff != "RIFF")
            {
                _logger.LogWarning("Gecersiz WAV dosyasi (RIFF yok): {Path}", filePath);
                return null;
            }
            reader.ReadInt32(); // file size
            var wave = new string(reader.ReadChars(4));
            if (wave != "WAVE")
            {
                _logger.LogWarning("Gecersiz WAV dosyasi (WAVE yok): {Path}", filePath);
                return null;
            }

            // Chunk'lari tara, "data" chunk'i bul
            while (stream.Position < stream.Length - 8)
            {
                var chunkId = new string(reader.ReadChars(4));
                var chunkSize = reader.ReadInt32();

                if (chunkId == "data")
                {
                    return reader.ReadBytes(chunkSize);
                }

                // Diger chunk'lari atla (fmt, LIST, etc.)
                stream.Position += chunkSize;
                // Pad byte (chunk boyutu tek ise)
                if (chunkSize % 2 != 0 && stream.Position < stream.Length)
                    stream.Position++;
            }

            _logger.LogWarning("WAV dosyasinda data chunk bulunamadi: {Path}", filePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WAV dosya okuma hatasi: {Path}", filePath);
            return null;
        }
    }

    /// <summary>16-bit PCM -> mu-law encode (ITU-T G.711)</summary>
    private static byte[] EncodePcmToMuLaw(Span<byte> pcmData)
    {
        var sampleCount = pcmData.Length / BytesPerSample;
        var encoded = new byte[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            var sample = (short)(pcmData[i * 2] | (pcmData[i * 2 + 1] << 8));
            encoded[i] = LinearToMuLaw(sample);
        }

        return encoded;
    }

    /// <summary>ITU-T G.711 mu-law encoding</summary>
    private static byte LinearToMuLaw(short sample)
    {
        const int MuLawMax = 0x1FFF;
        const int MuLawBias = 0x84;

        var sign = (sample >> 8) & 0x80;
        if (sign != 0) sample = (short)-sample;
        if (sample > MuLawMax) sample = MuLawMax;

        sample = (short)(sample + MuLawBias);

        var exponent = 7;
        for (var expMask = 0x4000; (sample & expMask) == 0 && exponent > 0; exponent--, expMask >>= 1) { }

        var mantissa = (sample >> (exponent + 3)) & 0x0F;
        var muLawByte = (byte)(sign | (exponent << 4) | mantissa);

        return (byte)~muLawByte;
    }
}
