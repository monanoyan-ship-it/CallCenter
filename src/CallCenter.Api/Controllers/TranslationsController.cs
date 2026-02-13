using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class TranslationsController : AuditableControllerBase
{
    public TranslationsController(ServiceFactory factory) : base(factory) { }

    /// <summary>
    /// Tum cevirileri belirtilen dilde JSON olarak dondurur (frontend icin)
    /// </summary>
    [HttpGet("{languageCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(string languageCode)
    {
        var svc = Factory.CreateTranslationManagementService();
        return Ok(await svc.GetAllTranslationsAsync(languageCode));
    }

    /// <summary>
    /// Tum cevirileri XML olarak indir
    /// </summary>
    [HttpGet("export/xml")]
    public async Task<IActionResult> ExportXml()
    {
        var svc = Factory.CreateTranslationManagementService();
        var bytes = await svc.ExportXmlAsync();
        return File(bytes, "application/xml", "translations.xml");
    }

    /// <summary>
    /// XML dosyasindan cevirileri yukle (mevcut olanlari guncelle, olmayanlari ekle)
    /// </summary>
    [HttpPost("import/xml")]
    public async Task<IActionResult> ImportXml(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Dosya secilmedi." });

        using var stream = file.OpenReadStream();
        var svc = Factory.CreateTranslationManagementService();
        var (success, message) = await svc.ImportXmlAsync(stream, User.Identity?.Name);

        if (!success)
            return BadRequest(new { message });

        await AuditCrudAsync("Import", "Translation", null,
            $"Ceviri XML import edildi: {file.FileName}");

        return Ok(new { message });
    }

    /// <summary>
    /// Cache'i yenile (moderator DB'den degistirirse)
    /// </summary>
    [HttpPost("reload-cache")]
    public async Task<IActionResult> ReloadCache()
    {
        var svc = Factory.CreateTranslationManagementService();
        await svc.ReloadCacheAsync();
        return Ok(new { message = "Ceviri onbellegi yenilendi." });
    }

    // ═══════════════════════════════════════════════════════════
    // KEY CRUD (Admin panel icin)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Dil listesi</summary>
    [HttpGet("languages")]
    public async Task<ActionResult<List<LanguageDto>>> GetLanguages()
    {
        var svc = Factory.CreateTranslationManagementService();
        return Ok(await svc.GetLanguagesAsync());
    }

    /// <summary>Key listesi (tum dillerdeki degerlerle birlikte)</summary>
    [HttpGet("keys")]
    public async Task<ActionResult<PagedResult<TranslationKeyListDto>>> GetKeys(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        [FromQuery] string? search = null,
        [FromQuery] string? module = null)
    {
        var svc = Factory.CreateTranslationManagementService();
        return Ok(await svc.GetKeysAsync(page, pageSize, search, module));
    }

    /// <summary>Yeni key + ceviri olustur</summary>
    [HttpPost("keys")]
    public async Task<ActionResult> CreateKey(TranslationKeyCreateDto dto)
    {
        var svc = Factory.CreateTranslationManagementService();
        var (success, id, error) = await svc.CreateKeyAsync(dto, User.Identity?.Name);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "TranslationKey", id.ToString(),
            $"Ceviri key'i olusturuldu: '{dto.Key}' ({dto.Module})");

        return Ok(new { id });
    }

    /// <summary>Key + ceviri guncelle</summary>
    [HttpPut("keys/{id}")]
    public async Task<ActionResult> UpdateKey(int id, TranslationKeyUpdateDto dto)
    {
        var svc = Factory.CreateTranslationManagementService();
        var (success, error) = await svc.UpdateKeyAsync(id, dto, User.Identity?.Name);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Update", "TranslationKey", id.ToString(),
            $"Ceviri key'i guncellendi: ID={id}");

        return NoContent();
    }

    /// <summary>Key sil (cascading: ceviriler de silinir)</summary>
    [HttpDelete("keys/{id}")]
    public async Task<ActionResult> DeleteKey(int id)
    {
        var svc = Factory.CreateTranslationManagementService();
        var (success, error) = await svc.DeleteKeyAsync(id);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Delete", "TranslationKey", id.ToString(),
            $"Ceviri key'i silindi: ID={id}");

        return NoContent();
    }
}
