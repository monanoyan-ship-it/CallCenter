namespace CallCenter.Shared.Services;

public interface ITranslationService
{
    string T(string key, string? defaultText = null);
    Task<Dictionary<string, string>> GetAllAsync(string languageCode);
    Task ReloadCacheAsync();
    string CurrentLanguage { get; set; }
}
