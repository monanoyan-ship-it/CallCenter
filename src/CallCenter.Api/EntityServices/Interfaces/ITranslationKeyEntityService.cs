using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ITranslationKeyEntityService
{
    IQueryable<TranslationKey> GetKeysQueryable();
    IQueryable<Translation> GetTranslationsQueryable();
    IQueryable<Language> GetLanguagesQueryable();
    Task<TranslationKey?> GetKeyByIdWithTranslationsAsync(int id);
    Task<bool> ExistsByKeyAsync(string key);
    void AddKey(TranslationKey entity);
    void RemoveKey(TranslationKey entity);
    void AddTranslation(Translation entity);
    void RemoveTranslations(IEnumerable<Translation> entities);
}
