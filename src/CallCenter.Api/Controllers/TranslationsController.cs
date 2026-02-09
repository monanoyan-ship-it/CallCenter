using System.Xml.Linq;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Services;
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
}
