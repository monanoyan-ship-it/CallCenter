using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class TranslationsController : AuditableControllerBase
{
    private readonly ITranslationFactory _translationFactory;

    public TranslationsController(IAuditFactory auditFactory, ITranslationFactory translationFactory) : base(auditFactory)
    {
        _translationFactory = translationFactory;
    }

    [HttpGet("{languageCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(string languageCode, [FromQuery] string? module = null, [FromQuery] int? platformId = null)
    {
        return Ok(await _translationFactory.GetAllTranslationsAsync(languageCode, module, platformId));
    }

    [HttpGet("export/xml")]
    public async Task<IActionResult> ExportXml()
    {
        var bytes = await _translationFactory.ExportXmlAsync();
        return File(bytes, "application/xml", "translations.xml");
    }

    [HttpPost("import/xml")]
    public async Task<IActionResult> ImportXml(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Dosya secilmedi." });

        using var stream = file.OpenReadStream();
        var (success, message) = await _translationFactory.ImportXmlAsync(stream, User.Identity?.Name);

        if (!success)
            return BadRequest(new { message });

        await AuditCrudAsync("Import", "Translation", null,
            $"Ceviri XML import edildi: {file.FileName}");

        return Ok(new { message });
    }

    [HttpPost("reload-cache")]
    public async Task<IActionResult> ReloadCache()
    {
        await _translationFactory.ReloadCacheAsync();
        return Ok(new { message = "Ceviri onbellegi yenilendi." });
    }

    [HttpGet("platforms")]
    public IActionResult GetPlatforms()
    {
        var platforms = CallCenter.Shared.Enums.TranslationPlatforms.All
            .Select(p => new { p.Id, name = p.SystemName, displayName = p.Description, p.Icon })
            .ToList();
        return Ok(platforms);
    }

    [HttpGet("languages")]
    public async Task<ActionResult<List<LanguageDto>>> GetLanguages()
    {
        return Ok(await _translationFactory.GetLanguagesAsync());
    }

    [HttpGet("keys")]
    public async Task<ActionResult<PagedResult<TranslationKeyListDto>>> GetKeys(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        [FromQuery] string? search = null,
        [FromQuery] string? module = null,
        [FromQuery] int? platformId = null)
    {
        return Ok(await _translationFactory.GetKeysAsync(page, pageSize, search, module, platformId));
    }

    [HttpPost("keys")]
    public async Task<ActionResult> CreateKey(TranslationKeyCreateDto dto)
    {
        var (success, id, error) = await _translationFactory.CreateKeyAsync(dto, User.Identity?.Name);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "TranslationKey", id.ToString(),
            $"Ceviri key'i olusturuldu: '{dto.Key}' ({dto.Module})");

        return Ok(new { id });
    }

    [HttpPut("keys/{id}")]
    public async Task<ActionResult> UpdateKey(int id, TranslationKeyUpdateDto dto)
    {
        var (success, error) = await _translationFactory.UpdateKeyAsync(id, dto, User.Identity?.Name);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Update", "TranslationKey", id.ToString(),
            $"Ceviri key'i guncellendi: ID={id}");

        return NoContent();
    }

    [HttpDelete("keys/{id}")]
    public async Task<ActionResult> DeleteKey(int id)
    {
        var (success, error) = await _translationFactory.DeleteKeyAsync(id);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Delete", "TranslationKey", id.ToString(),
            $"Ceviri key'i silindi: ID={id}");

        return NoContent();
    }
}
