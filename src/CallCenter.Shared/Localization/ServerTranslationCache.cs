using System.Collections.Concurrent;
using System.Net.Http.Json;

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
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);

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

    public void Clear()
    {
        _cache.Clear();
    }

    private record CacheEntry(Dictionary<string, string> Translations, DateTime LoadedAt)
    {
        public bool IsExpired => DateTime.UtcNow - LoadedAt > CacheTtl;
    }
}
