using System.Security.Cryptography;

namespace CallCenter.PbxService.Services;

/// <summary>
/// Sunucu tarafli cagri kaydi.
/// RTP stream'lerden ses verisi yakalar, stereo WAV olusturur.
/// Sol kanal: arayan, sag kanal: agent.
/// Kayit sonrasi AES-256-CBC ile sifreler, SHA-256 hash hesaplar.
/// </summary>
public class CallRecordingService
{
    private readonly ILogger<CallRecordingService> _logger;
    private readonly IApiClient _apiClient;
    private readonly string _recordingsDir;

    public CallRecordingService(ILogger<CallRecordingService> logger, IApiClient apiClient)
    {
        _logger = logger;
        _apiClient = apiClient;
        _recordingsDir = Path.Combine(AppContext.BaseDirectory, "recordings");
        Directory.CreateDirectory(_recordingsDir);
    }

    /// <summary>Yeni kayit oturumu baslat</summary>
    public RecordingSession StartRecording(string callId)
    {
        var session = new RecordingSession
        {
            CallId = callId,
            StartedAt = DateTime.UtcNow,
            CallerPcmBuffer = new MemoryStream(),
            AgentPcmBuffer = new MemoryStream()
        };

        _logger.LogInformation("Kayit baslatildi: {CallId}", callId);
        return session;
    }

    /// <summary>Arayan taraftan gelen RTP ses verisini kaydet</summary>
    public void WriteCallerAudio(RecordingSession session, byte[] pcmData)
    {
        session.CallerPcmBuffer.Write(pcmData, 0, pcmData.Length);
    }

    /// <summary>Agent taraftan gelen RTP ses verisini kaydet</summary>
    public void WriteAgentAudio(RecordingSession session, byte[] pcmData)
    {
        session.AgentPcmBuffer.Write(pcmData, 0, pcmData.Length);
    }

    /// <summary>Kaydi durdur, WAV olustur, sifrele, hash hesapla</summary>
    public async Task<RecordingResult?> StopRecordingAsync(
        RecordingSession session, int? callRecordId)
    {
        try
        {
            session.EndedAt = DateTime.UtcNow;
            var durationSeconds = (int)(session.EndedAt.Value - session.StartedAt).TotalSeconds;

            // PCM buffer'lari byte[] olarak al
            var callerPcm = session.CallerPcmBuffer.ToArray();
            var agentPcm = session.AgentPcmBuffer.ToArray();

            if (callerPcm.Length == 0 && agentPcm.Length == 0)
            {
                _logger.LogWarning("Kayit verisi bos: {CallId}", session.CallId);
                return null;
            }

            // Stereo WAV olustur (sol: arayan, sag: agent)
            var stereoWav = CreateStereoWav(callerPcm, agentPcm);

            // Dosyaya yaz
            var fileName = $"{session.CallId}_{session.StartedAt:yyyyMMdd_HHmmss}.wav";
            var wavPath = Path.Combine(_recordingsDir, fileName);
            await File.WriteAllBytesAsync(wavPath, stereoWav);

            // SHA-256 hash
            var hash = ComputeSha256Hash(stereoWav);

            // AES-256-CBC ile sifrele
            var encPath = wavPath + ".enc";
            await EncryptFileAsync(wavPath, encPath);

            // Orijinal WAV sil (sadece sifreli kalsin)
            File.Delete(wavPath);

            var fileSize = new FileInfo(encPath).Length;

            _logger.LogInformation(
                "Kayit tamamlandi: {CallId}, Sure={Duration}s, Boyut={Size} bytes",
                session.CallId, durationSeconds, fileSize);

            // API'ye bildir
            if (callRecordId.HasValue)
            {
                await _apiClient.UpdateCallRecordAsync(callRecordId.Value, new CallRecordUpdate
                {
                    // RecordingFilePath, Hash, Size API tarafinda baska endpoint ile guncellenebilir
                });
            }

            return new RecordingResult
            {
                FilePath = encPath,
                FileHash = hash,
                FileSize = fileSize,
                DurationSeconds = durationSeconds,
                IsEncrypted = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kayit durdurma hatasi: {CallId}", session.CallId);
            return null;
        }
        finally
        {
            session.CallerPcmBuffer.Dispose();
            session.AgentPcmBuffer.Dispose();
        }
    }

    /// <summary>
    /// Stereo WAV olustur: sol kanal arayan, sag kanal agent.
    /// Format: 8000 Hz, 16-bit, stereo.
    /// </summary>
    private static byte[] CreateStereoWav(byte[] callerPcm, byte[] agentPcm)
    {
        // Iki kanali ayni uzunluga getir
        var maxLength = Math.Max(callerPcm.Length, agentPcm.Length);
        var callerPadded = new byte[maxLength];
        var agentPadded = new byte[maxLength];
        Array.Copy(callerPcm, callerPadded, callerPcm.Length);
        Array.Copy(agentPcm, agentPadded, agentPcm.Length);

        // 16-bit mono PCM -> 16-bit stereo PCM interleave
        // Her sample 2 byte (16-bit), stereo = 4 byte per frame
        var sampleCount = maxLength / 2; // 16-bit samples
        var stereoData = new byte[sampleCount * 4]; // 2 channels * 2 bytes

        for (var i = 0; i < sampleCount; i++)
        {
            var srcOffset = i * 2;
            var dstOffset = i * 4;

            // Sol kanal (arayan)
            if (srcOffset + 1 < callerPadded.Length)
            {
                stereoData[dstOffset] = callerPadded[srcOffset];
                stereoData[dstOffset + 1] = callerPadded[srcOffset + 1];
            }

            // Sag kanal (agent)
            if (srcOffset + 1 < agentPadded.Length)
            {
                stereoData[dstOffset + 2] = agentPadded[srcOffset];
                stereoData[dstOffset + 3] = agentPadded[srcOffset + 1];
            }
        }

        // WAV header olustur
        return CreateWavFile(stereoData, sampleRate: 8000, bitsPerSample: 16, channels: 2);
    }

    private static byte[] CreateWavFile(byte[] pcmData, int sampleRate, int bitsPerSample, int channels)
    {
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var blockAlign = channels * bitsPerSample / 8;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // RIFF header
        bw.Write("RIFF"u8);
        bw.Write(36 + pcmData.Length); // File size - 8
        bw.Write("WAVE"u8);

        // fmt chunk
        bw.Write("fmt "u8);
        bw.Write(16); // Chunk size
        bw.Write((short)1); // PCM format
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write((short)blockAlign);
        bw.Write((short)bitsPerSample);

        // data chunk
        bw.Write("data"u8);
        bw.Write(pcmData.Length);
        bw.Write(pcmData);

        return ms.ToArray();
    }

    private static string ComputeSha256Hash(byte[] data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexStringLower(hashBytes);
    }

    private static async Task EncryptFileAsync(string inputPath, string outputPath)
    {
        // AES-256-CBC: [16 byte IV][ciphertext]
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.GenerateIV();
        aes.GenerateKey(); // TODO: Shared key kullanilmali (Encryption:Key config)

        await using var outStream = File.Create(outputPath);

        // IV yaz
        await outStream.WriteAsync(aes.IV);

        // Sifrele
        using var encryptor = aes.CreateEncryptor();
        await using var cryptoStream = new CryptoStream(outStream, encryptor, CryptoStreamMode.Write);
        var plainData = await File.ReadAllBytesAsync(inputPath);
        await cryptoStream.WriteAsync(plainData);
        await cryptoStream.FlushFinalBlockAsync();
    }
}

/// <summary>Aktif kayit oturumu</summary>
public class RecordingSession
{
    public string CallId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public MemoryStream CallerPcmBuffer { get; set; } = null!;
    public MemoryStream AgentPcmBuffer { get; set; } = null!;
}

/// <summary>Tamamlanan kayit sonucu</summary>
public class RecordingResult
{
    public string FilePath { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int DurationSeconds { get; set; }
    public bool IsEncrypted { get; set; }
}
