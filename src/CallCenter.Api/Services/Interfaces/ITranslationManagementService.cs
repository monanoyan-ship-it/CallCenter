using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Interfaces;

/// <summary>
/// Translation CRUD islemleri icin servis.
/// Mevcut ITranslationService (cache/lookup servisi) dokunulmadi.
/// </summary>
public interface ITranslationManagementService
{
    Task<Dictionary<string, string>> GetAllTranslationsAsync(string languageCode);
    Task<byte[]> ExportXmlAsync();
    Task<(bool Success, string? Message)> ImportXmlAsync(Stream xmlStream, string? userName);
    Task ReloadCacheAsync();
    Task<List<LanguageDto>> GetLanguagesAsync();
    Task<PagedResult<TranslationKeyListDto>> GetKeysAsync(int page, int pageSize, string? search, string? module);
    Task<(bool Success, int? Id, string? Error)> CreateKeyAsync(TranslationKeyCreateDto dto, string? userName);
    Task<(bool Success, string? Error)> UpdateKeyAsync(int id, TranslationKeyUpdateDto dto, string? userName);
    Task<(bool Success, string? Error)> DeleteKeyAsync(int id);
}
