using CallCenter.Shared.Services;
using Google.Cloud.TextToSpeech.V1;

namespace CallCenter.Api.Services;

public class GoogleTtsService : ITtsService
{
    private readonly TextToSpeechClient _client;
    private readonly ILogger<GoogleTtsService> _logger;

    // Varsayilan sesler (dil bazli)
    private static readonly Dictionary<string, string> DefaultVoices = new()
    {
        ["tr-TR"] = "tr-TR-Standard-A",
        ["en-US"] = "en-US-Standard-C",
        ["en-GB"] = "en-GB-Standard-A",
        ["de-DE"] = "de-DE-Standard-A",
        ["fr-FR"] = "fr-FR-Standard-A",
        ["ar-XA"] = "ar-XA-Standard-A"
    };

    public GoogleTtsService(TextToSpeechClient client, ILogger<GoogleTtsService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<int> SynthesizeToFileAsync(string text, string language, string? voice, string outputPath)
    {
        if (_client == null)
            throw new InvalidOperationException("TTS servisi yapilandirilmamis. Google Cloud credentials gerekli.");

        var selectedVoice = voice;
        if (string.IsNullOrEmpty(selectedVoice))
            DefaultVoices.TryGetValue(language, out selectedVoice);
        selectedVoice ??= $"{language}-Standard-A";

        var request = new SynthesizeSpeechRequest
        {
            Input = new SynthesisInput { Text = text },
            Voice = new VoiceSelectionParams
            {
                LanguageCode = language,
                Name = selectedVoice
            },
            AudioConfig = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Linear16,
                SampleRateHertz = 8000
            }
        };

        var response = await _client.SynthesizeSpeechAsync(request);
        var audioBytes = response.AudioContent.ToByteArray();

        // Google LINEAR16 raw PCM doner (WAV header yok), WAV header ekle
        var wavBytes = AddWavHeader(audioBytes, sampleRate: 8000, bitsPerSample: 16, channels: 1);
        await File.WriteAllBytesAsync(outputPath, wavBytes);

        var durationSeconds = audioBytes.Length / (8000 * 2); // 8kHz * 16bit/8
        _logger.LogInformation("TTS: '{Text}' -> {Path} ({Duration}s, voice={Voice})",
            text.Length > 50 ? text[..50] + "..." : text, outputPath, durationSeconds, selectedVoice);

        return durationSeconds;
    }

    public Task<List<TtsLanguageInfo>> GetSupportedLanguagesAsync()
    {
        var languages = new List<TtsLanguageInfo>
        {
            new() { Code = "tr-TR", Name = "Turkce" },
            new() { Code = "en-US", Name = "English (US)" },
            new() { Code = "en-GB", Name = "English (UK)" },
            new() { Code = "de-DE", Name = "Deutsch" },
            new() { Code = "fr-FR", Name = "Francais" },
            new() { Code = "ar-XA", Name = "Arabic" }
        };
        return Task.FromResult(languages);
    }

    public async Task<List<TtsVoiceInfo>> GetVoicesAsync(string language)
    {
        if (_client == null)
            return GetFallbackVoices(language);

        try
        {
            var response = await _client.ListVoicesAsync(new ListVoicesRequest { LanguageCode = language });
            return response.Voices.Select(v => new TtsVoiceInfo
            {
                Name = v.Name,
                DisplayName = $"{v.Name} ({v.SsmlGender})",
                Gender = v.SsmlGender.ToString()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TTS voice listesi alinamadi: {Language}", language);
            return GetFallbackVoices(language);
        }
    }

    private static List<TtsVoiceInfo> GetFallbackVoices(string language)
    {
        var prefix = language;
        return
        [
            new() { Name = $"{prefix}-Standard-A", DisplayName = $"{prefix} Standard A (Kadin)", Gender = "Female" },
            new() { Name = $"{prefix}-Standard-B", DisplayName = $"{prefix} Standard B (Erkek)", Gender = "Male" },
            new() { Name = $"{prefix}-Standard-C", DisplayName = $"{prefix} Standard C (Kadin)", Gender = "Female" },
            new() { Name = $"{prefix}-Standard-D", DisplayName = $"{prefix} Standard D (Erkek)", Gender = "Male" },
            new() { Name = $"{prefix}-Wavenet-A", DisplayName = $"{prefix} WaveNet A (Kadin)", Gender = "Female" },
            new() { Name = $"{prefix}-Wavenet-B", DisplayName = $"{prefix} WaveNet B (Erkek)", Gender = "Male" }
        ];
    }

    private static byte[] AddWavHeader(byte[] pcmData, int sampleRate, int bitsPerSample, int channels)
    {
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var blockAlign = channels * bitsPerSample / 8;

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // RIFF header
        writer.Write("RIFF"u8);
        writer.Write(36 + pcmData.Length); // ChunkSize
        writer.Write("WAVE"u8);

        // fmt subchunk
        writer.Write("fmt "u8);
        writer.Write(16);              // SubChunk1Size (PCM)
        writer.Write((short)1);        // AudioFormat (PCM)
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);

        // data subchunk
        writer.Write("data"u8);
        writer.Write(pcmData.Length);
        writer.Write(pcmData);

        return ms.ToArray();
    }
}
