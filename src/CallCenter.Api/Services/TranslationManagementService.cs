using System.Xml.Linq;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class TranslationManagementService : ITranslationManagementService
{
    private readonly AppDbContext _db;
    private readonly ITranslationService _translationService;

    public TranslationManagementService(AppDbContext db, ITranslationService translationService)
    {
        _db = db;
        _translationService = translationService;
    }

    public async Task<Dictionary<string, string>> GetAllTranslationsAsync(string languageCode)
    {
        return await _translationService.GetAllAsync(languageCode);
    }

    public async Task<byte[]> ExportXmlAsync()
    {
        var languages = await _db.Languages.Where(l => l.IsActive).ToListAsync();
        var keys = await _db.TranslationKeys
            .Include(tk => tk.Translations)
            .OrderBy(tk => tk.Module)
            .ThenBy(tk => tk.Key)
            .ToListAsync();

        var xml = new XElement("Translations",
            new XAttribute("ExportDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")),
            new XElement("Languages",
                languages.Select(l => new XElement("Language",
                    new XAttribute("Code", l.Code),
                    new XAttribute("Name", l.Name),
                    new XAttribute("IsDefault", l.IsDefault)
                ))
            ),
            new XElement("Keys",
                keys.Select(tk => new XElement("Key",
                    new XAttribute("Id", tk.Key),
                    new XAttribute("Module", tk.Module),
                    new XAttribute("Description", tk.Description ?? ""),
                    tk.Translations.Select(t => new XElement("Value",
                        new XAttribute("Lang", t.LanguageCode),
                        t.Value
                    ))
                ))
            )
        );

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), xml);
        using var stream = new MemoryStream();
        doc.Save(stream);
        return stream.ToArray();
    }

    public async Task<(bool Success, string? Message)> ImportXmlAsync(Stream xmlStream, string? userName)
    {
        var doc = await XDocument.LoadAsync(xmlStream, LoadOptions.None, CancellationToken.None);

        var root = doc.Element("Translations");
        if (root == null)
            return (false, "Geçersiz XML formatı.");

        var keysElement = root.Element("Keys");
        if (keysElement == null)
            return (false, "Keys elementi bulunamadı.");

        int updated = 0;
        int added = 0;

        foreach (var keyElement in keysElement.Elements("Key"))
        {
            var keyId = keyElement.Attribute("Id")?.Value;
            var module = keyElement.Attribute("Module")?.Value ?? "common";
            var description = keyElement.Attribute("Description")?.Value;

            if (string.IsNullOrEmpty(keyId)) continue;

            var translationKey = await _db.TranslationKeys
                .FirstOrDefaultAsync(tk => tk.Key == keyId);

            if (translationKey == null)
            {
                translationKey = new TranslationKey
                {
                    Key = keyId,
                    Module = module,
                    Description = description
                };
                _db.TranslationKeys.Add(translationKey);
                await _db.SaveChangesAsync();
            }

            foreach (var valueElement in keyElement.Elements("Value"))
            {
                var lang = valueElement.Attribute("Lang")?.Value;
                var value = valueElement.Value;

                if (string.IsNullOrEmpty(lang)) continue;

                var translation = await _db.Translations
                    .FirstOrDefaultAsync(t => t.TranslationKeyId == translationKey.Id && t.LanguageCode == lang);

                if (translation != null)
                {
                    translation.Value = value;
                    translation.UpdatedAt = DateTime.UtcNow;
                    translation.UpdatedBy = userName ?? "import";
                    updated++;
                }
                else
                {
                    _db.Translations.Add(new Translation
                    {
                        TranslationKeyId = translationKey.Id,
                        LanguageCode = lang,
                        Value = value,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = userName ?? "import"
                    });
                    added++;
                }
            }
        }

        await _db.SaveChangesAsync();
        await _translationService.ReloadCacheAsync();

        return (true, $"İçe aktarma tamamlandı. {added} eklendi, {updated} güncellendi.");
    }

    public async Task ReloadCacheAsync()
    {
        await _translationService.ReloadCacheAsync();
    }

    public async Task<List<LanguageDto>> GetLanguagesAsync()
    {
        return await _db.Languages
            .Where(l => l.IsActive)
            .OrderByDescending(l => l.IsDefault)
            .ThenBy(l => l.Name)
            .Select(l => new LanguageDto
            {
                Code = l.Code,
                Name = l.Name,
                IsDefault = l.IsDefault,
                IsActive = l.IsActive
            })
            .ToListAsync();
    }

    public async Task<PagedResult<TranslationKeyListDto>> GetKeysAsync(int page, int pageSize, string? search, string? module)
    {
        var query = _db.TranslationKeys.Include(tk => tk.Translations).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(tk => tk.Key.ToLower().Contains(s)
                                   || tk.Translations.Any(t => t.Value.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(tk => tk.Module == module);
        }

        var totalCount = await query.CountAsync();

        var rawItems = await query
            .OrderBy(tk => tk.Module).ThenBy(tk => tk.Key)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(tk => new
            {
                tk.Id,
                tk.Key,
                tk.Module,
                tk.Description,
                Translations = tk.Translations.Select(t => new { t.LanguageCode, t.Value })
            })
            .ToListAsync();

        var items = rawItems.Select(tk => new TranslationKeyListDto
        {
            Id = tk.Id,
            Key = tk.Key,
            Module = tk.Module,
            Description = tk.Description,
            Values = tk.Translations.ToDictionary(t => t.LanguageCode, t => t.Value)
        }).ToList();

        return new PagedResult<TranslationKeyListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<(bool Success, int? Id, string? Error)> CreateKeyAsync(TranslationKeyCreateDto dto, string? userName)
    {
        if (await _db.TranslationKeys.AnyAsync(tk => tk.Key == dto.Key))
            return (false, null, "Bu key zaten mevcut.");

        var translationKey = new TranslationKey
        {
            Key = dto.Key,
            Module = dto.Module,
            Description = dto.Description
        };

        _db.TranslationKeys.Add(translationKey);
        await _db.SaveChangesAsync();

        foreach (var (langCode, value) in dto.Values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _db.Translations.Add(new Translation
                {
                    TranslationKeyId = translationKey.Id,
                    LanguageCode = langCode,
                    Value = value,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = userName ?? "admin"
                });
            }
        }
        await _db.SaveChangesAsync();

        await _translationService.ReloadCacheAsync();

        return (true, translationKey.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateKeyAsync(int id, TranslationKeyUpdateDto dto, string? userName)
    {
        var translationKey = await _db.TranslationKeys
            .Include(tk => tk.Translations)
            .FirstOrDefaultAsync(tk => tk.Id == id);

        if (translationKey == null)
            return (false, "Key bulunamadi.");

        if (!string.IsNullOrWhiteSpace(dto.Module))
            translationKey.Module = dto.Module;

        if (dto.Description != null)
            translationKey.Description = dto.Description;

        foreach (var (langCode, value) in dto.Values)
        {
            var existing = translationKey.Translations.FirstOrDefault(t => t.LanguageCode == langCode);
            if (existing != null)
            {
                existing.Value = value;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = userName ?? "admin";
            }
            else if (!string.IsNullOrWhiteSpace(value))
            {
                _db.Translations.Add(new Translation
                {
                    TranslationKeyId = translationKey.Id,
                    LanguageCode = langCode,
                    Value = value,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = userName ?? "admin"
                });
            }
        }

        await _db.SaveChangesAsync();
        await _translationService.ReloadCacheAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteKeyAsync(int id)
    {
        var translationKey = await _db.TranslationKeys
            .Include(tk => tk.Translations)
            .FirstOrDefaultAsync(tk => tk.Id == id);

        if (translationKey == null)
            return (false, "Key bulunamadi.");

        _db.Translations.RemoveRange(translationKey.Translations);
        _db.TranslationKeys.Remove(translationKey);
        await _db.SaveChangesAsync();

        await _translationService.ReloadCacheAsync();

        return (true, null);
    }
}
