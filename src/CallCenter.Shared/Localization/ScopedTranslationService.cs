using CallCenter.Shared.Services;

namespace CallCenter.Shared.Localization;

/// <summary>
/// Scoped — her request icin ayri instance, CurrentLanguage request bazinda set edilir.
/// Cache'i ServerTranslationCache singleton'dan okur.
/// </summary>
public class ScopedTranslationService : ITranslationService
{
    private readonly ServerTranslationCache _cache;
    private Dictionary<string, string>? _translations;

    public ScopedTranslationService(ServerTranslationCache cache)
    {
        _cache = cache;
    }

    public string CurrentLanguage { get; set; } = "tr";

    public string T(string key, string? defaultText = null)
    {
        if (_translations != null && _translations.TryGetValue(key, out var value))
            return value;

        return defaultText ?? $"[{key}]";
    }

    public async Task<Dictionary<string, string>> GetAllAsync(string languageCode)
    {
        return await _cache.GetTranslationsAsync(languageCode);
    }

    public async Task ReloadCacheAsync()
    {
        _cache.Clear();
        _translations = await _cache.GetTranslationsAsync(CurrentLanguage);
    }

    /// <summary>
    /// Middleware tarafindan cagrilir — mevcut dil icin cevirileri yukler.
    /// </summary>
    public async Task LoadForCurrentLanguageAsync()
    {
        _translations = await _cache.GetTranslationsAsync(CurrentLanguage);
    }
}
