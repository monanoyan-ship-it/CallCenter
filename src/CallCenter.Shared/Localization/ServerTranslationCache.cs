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
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public ServerTranslationCache(HttpClient httpClient)
    {
        _http = httpClient;
    }

    public async Task<Dictionary<string, string>> GetTranslationsAsync(string languageCode)
    {
        if (_cache.TryGetValue(languageCode, out var entry) && !entry.IsExpired)
            return entry.Translations;

        try
        {
            var translations = await _http.GetFromJsonAsync<Dictionary<string, string>>(
                $"api/translations/{languageCode}") ?? new();

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
