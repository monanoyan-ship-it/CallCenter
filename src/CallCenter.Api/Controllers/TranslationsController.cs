using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class TranslationsController : ControllerBase
{
    private readonly ServiceFactory _factory;

    public TranslationsController(ServiceFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Tüm çevirileri belirtilen dilde JSON olarak döndürür (frontend için)
    /// </summary>
    [HttpGet("{languageCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(string languageCode)
    {
        var svc = _factory.CreateTranslationManagementService();
        return Ok(await svc.GetAllTranslationsAsync(languageCode));
    }

    /// <summary>
    /// Tüm çevirileri XML olarak indir
    /// </summary>
    [HttpGet("export/xml")]
    public async Task<IActionResult> ExportXml()
    {
        var svc = _factory.CreateTranslationManagementService();
        var bytes = await svc.ExportXmlAsync();
        return File(bytes, "application/xml", "translations.xml");
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
        var svc = _factory.CreateTranslationManagementService();
        var (success, message) = await svc.ImportXmlAsync(stream, User.Identity?.Name);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    /// <summary>
    /// Cache'i yenile (moderatör DB'den değiştirirse)
    /// </summary>
    [HttpPost("reload-cache")]
    public async Task<IActionResult> ReloadCache()
    {
        var svc = _factory.CreateTranslationManagementService();
        await svc.ReloadCacheAsync();
        return Ok(new { message = "Çeviri önbelleği yenilendi." });
    }

    // ═══════════════════════════════════════════════════════════
    // KEY CRUD (Admin panel icin)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Dil listesi</summary>
    [HttpGet("languages")]
    public async Task<ActionResult<List<LanguageDto>>> GetLanguages()
    {
        var svc = _factory.CreateTranslationManagementService();
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
        var svc = _factory.CreateTranslationManagementService();
        return Ok(await svc.GetKeysAsync(page, pageSize, search, module));
    }

    /// <summary>Yeni key + ceviri olustur</summary>
    [HttpPost("keys")]
    public async Task<ActionResult> CreateKey(TranslationKeyCreateDto dto)
    {
        var svc = _factory.CreateTranslationManagementService();
        var (success, id, error) = await svc.CreateKeyAsync(dto, User.Identity?.Name);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { id });
    }

    /// <summary>Key + ceviri guncelle</summary>
    [HttpPut("keys/{id}")]
    public async Task<ActionResult> UpdateKey(int id, TranslationKeyUpdateDto dto)
    {
        var svc = _factory.CreateTranslationManagementService();
        var (success, error) = await svc.UpdateKeyAsync(id, dto, User.Identity?.Name);
        if (!success) return NotFound(new { message = error });
        return NoContent();
    }

    /// <summary>Key sil (cascading: ceviriler de silinir)</summary>
    [HttpDelete("keys/{id}")]
    public async Task<ActionResult> DeleteKey(int id)
    {
        var svc = _factory.CreateTranslationManagementService();
        var (success, error) = await svc.DeleteKeyAsync(id);
        if (!success) return NotFound(new { message = error });
        return NoContent();
    }
}
