namespace CallCenter.Shared.Services;

public interface ITtsService
{
    /// <summary>
    /// Metni sese cevirir ve WAV dosyasi olarak kaydeder.
    /// Format: PCM 8kHz 16-bit mono (telefon kalitesi).
    /// </summary>
    /// <param name="text">Okunacak metin</param>
    /// <param name="language">Dil kodu (tr-TR, en-US vb.)</param>
    /// <param name="voice">Ses adi (provider'a ozel, null = varsayilan)</param>
    /// <param name="outputPath">Kaydedilecek WAV dosya yolu</param>
    /// <returns>Ses suresi (saniye)</returns>
    Task<int> SynthesizeToFileAsync(string text, string language, string? voice, string outputPath);

    /// <summary>Desteklenen dilleri dondurur.</summary>
    Task<List<TtsLanguageInfo>> GetSupportedLanguagesAsync();

    /// <summary>Desteklenen sesleri dondurur.</summary>
    Task<List<TtsVoiceInfo>> GetVoicesAsync(string language);
}

public class TtsLanguageInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class TtsVoiceInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
}
