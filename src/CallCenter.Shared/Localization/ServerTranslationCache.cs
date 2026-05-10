using System.Collections.Concurrent;
using System.Net.Http.Json;
using CallCenter.Shared.DTOs;

namespace CallCenter.Shared.Localization;

/// <summary>
/// Singleton cache — API'den ceviri yukler, bellekte tutar.
/// MVC uygulamalari (Salon, Management, CRM, Landing) icin.
/// </summary>
public class ServerTranslationCache
{
    private readonly HttpClient _http;
    private readonly string? _module;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private LanguageCacheEntry? _languageCache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LanguageCacheTtl = TimeSpan.FromMinutes(30);

    public ServerTranslationCache(HttpClient httpClient, string? module = null)
    {
        _http = httpClient;
        _http.Timeout = TimeSpan.FromSeconds(5);
        _module = module;
    }

    public async Task<Dictionary<string, string>> GetTranslationsAsync(string languageCode)
    {
        if (_cache.TryGetValue(languageCode, out var entry) && !entry.IsExpired)
            return entry.Translations;

        try
        {
            var url = $"api/translations/{languageCode}";
            if (!string.IsNullOrEmpty(_module)) url += $"?module={_module}";
            var translations = await _http.GetFromJsonAsync<Dictionary<string, string>>(url) ?? new();

            _cache[languageCode] = new CacheEntry(translations, DateTime.UtcNow);
            return translations;
        }
        catch
        {
            // API erisilemezse mevcut cache'i dondur (suresi dolmus olsa bile)
            if (entry != null)
                return entry.Translations;

            return new Dictionary<string, string>();
        }
    }

    public async Task<IReadOnlyList<LocalizationConstants.LanguageInfo>> GetLanguagesAsync()
    {
        if (_languageCache is { IsExpired: false })
            return _languageCache.Languages;

        try
        {
            var apiLanguages = await _http.GetFromJsonAsync<List<LanguageDto>>("api/translations/languages") ?? new();
            var languages = apiLanguages
                .Where(l => l.IsActive && !string.IsNullOrWhiteSpace(l.Code))
                .Select(l => new LocalizationConstants.LanguageInfo(
                    l.Code.Trim().ToLowerInvariant(),
                    string.IsNullOrWhiteSpace(l.Name) ? l.Code.Trim().ToUpperInvariant() : l.Name.Trim(),
                    string.IsNullOrWhiteSpace(l.Label) ? l.Code.Trim().ToUpperInvariant() : l.Label.Trim()))
                .ToList();

            if (languages.Count > 0)
            {
                _languageCache = new LanguageCacheEntry(languages, DateTime.UtcNow);
                return languages;
            }
        }
        catch
        {
            // API erisilemezse lokal sabit listeye duser; public sayfa dil menusu kirilmaz.
        }

        return LocalizationConstants.Languages.Values.ToList();
    }

    public void Clear()
    {
        _cache.Clear();
        _languageCache = null;
    }

    private record CacheEntry(Dictionary<string, string> Translations, DateTime LoadedAt)
    {
        public bool IsExpired => DateTime.UtcNow - LoadedAt > CacheTtl;
    }

    private record LanguageCacheEntry(IReadOnlyList<LocalizationConstants.LanguageInfo> Languages, DateTime LoadedAt)
    {
        public bool IsExpired => DateTime.UtcNow - LoadedAt > LanguageCacheTtl;
    }
}
