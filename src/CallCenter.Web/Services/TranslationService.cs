using System.Net.Http.Json;

namespace CallCenter.Web.Services;

/// <summary>
/// Frontend ceviri servisi. Backend API'den dil paketini ceker,
/// localStorage'da cache'ler, T(key) metodu ile ceviri dondurur.
/// </summary>
public class TranslationService
{
    private readonly HttpClient _http;
    private Dictionary<string, string> _translations = new();
    private string _currentLanguage = "tr";
    private bool _isLoaded;

    public string CurrentLanguage => _currentLanguage;
    public bool IsLoaded => _isLoaded;

    /// <summary>Dil degistiginde tetiklenir (UI yenileme icin).</summary>
    public event Action? OnLanguageChanged;

    public TranslationService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Belirtilen dildeki tum cevirileri API'den yukler.
    /// </summary>
    public async Task LoadAsync(string languageCode)
    {
        try
        {
            var translations = await _http.GetFromJsonAsync<Dictionary<string, string>>(
                $"api/translations/{languageCode}");

            if (translations != null)
            {
                _translations = translations;
                _currentLanguage = languageCode;
                _isLoaded = true;
                OnLanguageChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Translation] Dil paketi yuklenemedi ({languageCode}): {ex.Message}");
            // Hata durumunda mevcut cevirileri koru
        }
    }

    /// <summary>
    /// Ceviri key'ine karsilik gelen metni dondurur.
    /// Key bulunamazsa defaultText veya key'in kendisi doner.
    /// </summary>
    public string T(string key, string? defaultText = null)
    {
        if (string.IsNullOrEmpty(key)) return defaultText ?? string.Empty;

        if (_translations.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
            return value;

        return defaultText ?? key;
    }

    /// <summary>
    /// Ceviri key'ine karsilik gelen metni dondurur.
    /// Parametreli versiyon: T("Hello.{0}", "Merhaba {0}", "Ali") → "Merhaba Ali"
    /// </summary>
    public string T(string key, string? defaultText, params object[] args)
    {
        var template = T(key, defaultText);
        try
        {
            return string.Format(template, args);
        }
        catch
        {
            return template;
        }
    }

    /// <summary>Dili degistirir ve yeni cevirileri yukler.</summary>
    public async Task ChangeLanguageAsync(string languageCode)
    {
        if (_currentLanguage == languageCode && _isLoaded) return;
        await LoadAsync(languageCode);
    }

    /// <summary>Mevcut cevirileri temizler (logout icin).</summary>
    public void Clear()
    {
        _translations.Clear();
        _isLoaded = false;
    }
}
