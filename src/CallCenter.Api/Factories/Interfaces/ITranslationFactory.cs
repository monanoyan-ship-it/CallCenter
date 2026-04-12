using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ITranslationFactory
{
    Task<Dictionary<string, string>> GetAllTranslationsAsync(string languageCode, string? module = null, int? platformId = null);
    Task<byte[]> ExportXmlAsync();
    Task<(bool Success, string? Message)> ImportXmlAsync(Stream xmlStream, string? userName);
    Task ReloadCacheAsync();
    Task<List<LanguageDto>> GetLanguagesAsync();
    Task<PagedResult<TranslationKeyListDto>> GetKeysAsync(int page, int pageSize, string? search, string? module);
    Task<(bool Success, int? Id, string? Error)> CreateKeyAsync(TranslationKeyCreateDto dto, string? userName);
    Task<(bool Success, string? Error)> UpdateKeyAsync(int id, TranslationKeyUpdateDto dto, string? userName);
    Task<(bool Success, string? Error)> DeleteKeyAsync(int id);
}
