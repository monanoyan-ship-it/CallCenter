using System.Xml.Linq;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Services;
using DTOs = CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class TranslationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITranslationService _translationService;

    public TranslationsController(AppDbContext db, ITranslationService translationService)
    {
        _db = db;
        _translationService = translationService;
    }

    /// <summary>
    /// Tüm çevirileri belirtilen dilde JSON olarak döndürür (frontend için)
    /// </summary>
    [HttpGet("{languageCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(string languageCode)
    {
        var translations = await _translationService.GetAllAsync(languageCode);
        return Ok(translations);
    }

    /// <summary>
    /// Tüm çevirileri XML olarak indir
    /// </summary>
    [HttpGet("export/xml")]
    public async Task<IActionResult> ExportXml()
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
        var stream = new MemoryStream();
        doc.Save(stream);
        stream.Position = 0;

        return File(stream, "application/xml", "translations.xml");
    }

    /// <summary>
    /// XML dosyasından çevirileri yükle (mevcut olanları güncelle, olmayanları ekle)
    /// </summary>
    [HttpPost("import/xml")]
    public async Task<IActionResult> ImportXml(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Dosya seçilmedi." });

        using var stream = file.OpenReadStream();
        var doc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);

        var root = doc.Element("Translations");
        if (root == null)
            return BadRequest(new { message = "Geçersiz XML formatı." });

        var keysElement = root.Element("Keys");
        if (keysElement == null)
            return BadRequest(new { message = "Keys elementi bulunamadı." });

        int updated = 0;
        int added = 0;

        foreach (var keyElement in keysElement.Elements("Key"))
        {
            var keyId = keyElement.Attribute("Id")?.Value;
            var module = keyElement.Attribute("Module")?.Value ?? "common";
            var description = keyElement.Attribute("Description")?.Value;

            if (string.IsNullOrEmpty(keyId)) continue;

            // Key'i bul veya oluştur
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

            // Her dil için çeviriyi güncelle/ekle
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
                    translation.UpdatedBy = User.Identity?.Name ?? "import";
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
                        UpdatedBy = User.Identity?.Name ?? "import"
                    });
                    added++;
                }
            }
        }

        await _db.SaveChangesAsync();

        // Cache'i yenile
        await _translationService.ReloadCacheAsync();

        return Ok(new { message = $"İçe aktarma tamamlandı. {added} eklendi, {updated} güncellendi." });
    }

    /// <summary>
    /// Cache'i yenile (moderatör DB'den değiştirirse)
    /// </summary>
    [HttpPost("reload-cache")]
    public async Task<IActionResult> ReloadCache()
    {
        await _translationService.ReloadCacheAsync();
        return Ok(new { message = "Çeviri önbelleği yenilendi." });
    }

    // ═══════════════════════════════════════════════════════════
    // KEY CRUD (Admin panel icin)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Dil listesi</summary>
    [HttpGet("languages")]
    public async Task<ActionResult<List<DTOs.LanguageDto>>> GetLanguages()
    {
        var languages = await _db.Languages
            .Where(l => l.IsActive)
            .OrderByDescending(l => l.IsDefault)
            .ThenBy(l => l.Name)
            .Select(l => new DTOs.LanguageDto
            {
                Code = l.Code,
                Name = l.Name,
                IsDefault = l.IsDefault,
                IsActive = l.IsActive
            })
            .ToListAsync();

        return Ok(languages);
    }

    /// <summary>Key listesi (tum dillerdeki degerlerle birlikte)</summary>
    [HttpGet("keys")]
    public async Task<ActionResult<DTOs.PagedResult<DTOs.TranslationKeyListDto>>> GetKeys(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        [FromQuery] string? search = null,
        [FromQuery] string? module = null)
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

        var items = await query
            .OrderBy(tk => tk.Module).ThenBy(tk => tk.Key)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(tk => new DTOs.TranslationKeyListDto
            {
                Id = tk.Id,
                Key = tk.Key,
                Module = tk.Module,
                Description = tk.Description,
                Values = tk.Translations.ToDictionary(t => t.LanguageCode, t => t.Value)
            })
            .ToListAsync();

        return Ok(new DTOs.PagedResult<DTOs.TranslationKeyListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>Yeni key + ceviri olustur</summary>
    [HttpPost("keys")]
    public async Task<ActionResult> CreateKey(DTOs.TranslationKeyCreateDto dto)
    {
        if (await _db.TranslationKeys.AnyAsync(tk => tk.Key == dto.Key))
            return BadRequest(new { message = "Bu key zaten mevcut." });

        var translationKey = new TranslationKey
        {
            Key = dto.Key,
            Module = dto.Module,
            Description = dto.Description
        };

        _db.TranslationKeys.Add(translationKey);
        await _db.SaveChangesAsync();

        // Her dil icin ceviri ekle
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
                    UpdatedBy = User.Identity?.Name ?? "admin"
                });
            }
        }
        await _db.SaveChangesAsync();

        // Cache yenile
        await _translationService.ReloadCacheAsync();

        return Ok(new { id = translationKey.Id });
    }

    /// <summary>Key + ceviri guncelle</summary>
    [HttpPut("keys/{id}")]
    public async Task<ActionResult> UpdateKey(int id, DTOs.TranslationKeyUpdateDto dto)
    {
        var translationKey = await _db.TranslationKeys
            .Include(tk => tk.Translations)
            .FirstOrDefaultAsync(tk => tk.Id == id);

        if (translationKey == null)
            return NotFound(new { message = "Key bulunamadi." });

        if (!string.IsNullOrWhiteSpace(dto.Module))
            translationKey.Module = dto.Module;

        if (dto.Description != null)
            translationKey.Description = dto.Description;

        // Cevirileri guncelle/ekle
        foreach (var (langCode, value) in dto.Values)
        {
            var existing = translationKey.Translations.FirstOrDefault(t => t.LanguageCode == langCode);
            if (existing != null)
            {
                existing.Value = value;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = User.Identity?.Name ?? "admin";
            }
            else if (!string.IsNullOrWhiteSpace(value))
            {
                _db.Translations.Add(new Translation
                {
                    TranslationKeyId = translationKey.Id,
                    LanguageCode = langCode,
                    Value = value,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = User.Identity?.Name ?? "admin"
                });
            }
        }

        await _db.SaveChangesAsync();
        await _translationService.ReloadCacheAsync();

        return NoContent();
    }

    /// <summary>Key sil (cascading: ceviriler de silinir)</summary>
    [HttpDelete("keys/{id}")]
    public async Task<ActionResult> DeleteKey(int id)
    {
        var translationKey = await _db.TranslationKeys
            .Include(tk => tk.Translations)
            .FirstOrDefaultAsync(tk => tk.Id == id);

        if (translationKey == null)
            return NotFound(new { message = "Key bulunamadi." });

        _db.Translations.RemoveRange(translationKey.Translations);
        _db.TranslationKeys.Remove(translationKey);
        await _db.SaveChangesAsync();

        await _translationService.ReloadCacheAsync();

        return NoContent();
    }
}
