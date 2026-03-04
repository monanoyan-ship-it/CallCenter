using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class TranslationKeyEntityService : ITranslationKeyEntityService
{
    private readonly AppDbContext _db;

    public TranslationKeyEntityService(AppDbContext db) => _db = db;

    public IQueryable<TranslationKey> GetKeysQueryable()
        => _db.TranslationKeys.AsQueryable();

    public IQueryable<Translation> GetTranslationsQueryable()
        => _db.Translations.AsQueryable();

    public IQueryable<Language> GetLanguagesQueryable()
        => _db.Languages.AsQueryable();

    public Task<TranslationKey?> GetKeyByIdWithTranslationsAsync(int id)
        => _db.TranslationKeys
            .Include(tk => tk.Translations)
            .FirstOrDefaultAsync(tk => tk.Id == id);

    public Task<bool> ExistsByKeyAsync(string key)
        => _db.TranslationKeys.AnyAsync(tk => tk.Key == key);

    public void AddKey(TranslationKey entity) => _db.TranslationKeys.Add(entity);
    public void RemoveKey(TranslationKey entity) => _db.TranslationKeys.Remove(entity);
    public void AddTranslation(Translation entity) => _db.Translations.Add(entity);
    public void RemoveTranslations(IEnumerable<Translation> entities) => _db.Translations.RemoveRange(entities);
}
