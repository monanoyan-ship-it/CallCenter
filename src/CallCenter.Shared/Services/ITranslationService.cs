namespace CallCenter.Shared.Services;

public interface ITranslationService
{
    string T(string key);
    string T(string key, string languageCode);
    Task<Dictionary<string, string>> GetAllAsync(string languageCode);
    Task ReloadCacheAsync();
    string CurrentLanguage { get; set; }
}
