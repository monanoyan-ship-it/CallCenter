using System.Collections.Concurrent;
using CallCenter.Data;
using CallCenter.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class TranslationService : ITranslationService
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();
    private static bool _isLoaded;

    public string CurrentLanguage { get; set; } = "tr";

    public TranslationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string T(string key, string? defaultText = null)
    {
        if (!_isLoaded)
        {
            ReloadCacheAsync().GetAwaiter().GetResult();
        }

        if (_cache.TryGetValue(CurrentLanguage, out var translations))
        {
            if (translations.TryGetValue(key, out var value))
                return value;
        }

        return defaultText ?? $"[{key}]";
    }

    public async Task<Dictionary<string, string>> GetAllAsync(string languageCode)
    {
        if (!_isLoaded)
            await ReloadCacheAsync();

        return _cache.TryGetValue(languageCode, out var translations)
            ? new Dictionary<string, string>(translations)
            : new Dictionary<string, string>();
    }

    public async Task ReloadCacheAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var translations = await db.Translations
            .Include(t => t.TranslationKey)
            .ToListAsync();

        _cache.Clear();

        foreach (var group in translations.GroupBy(t => t.LanguageCode))
        {
            var dict = group.ToDictionary(
                t => t.TranslationKey.Key,
                t => t.Value
            );
            _cache[group.Key] = dict;
        }

        _isLoaded = true;
    }
}
