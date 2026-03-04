using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IvrController : AuditableControllerBase
{
    private readonly IIvrFactory _ivrFactory;
    private readonly IWebHostEnvironment _env;

    public IvrController(IAuditFactory auditFactory, IIvrFactory ivrFactory, IWebHostEnvironment env) : base(auditFactory)
    {
        _ivrFactory = ivrFactory;
        _env = env;
    }

    private int ResolveCustomerId(int? customerId)
    {
        if (customerId.HasValue && customerId.Value > 0) return customerId.Value;
        var id = CurrentCustomerId;
        if (id == null) throw new UnauthorizedAccessException("CustomerId bulunamadi.");
        return id.Value;
    }

    private string EnsureAudioDirectory()
    {
        var dir = Path.Combine(_env.ContentRootPath, "Data", "Audio");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [HttpGet("greetings")]
    public async Task<ActionResult<List<GreetingMessageDto>>> GetGreetings([FromQuery] int? customerId = null)
    {
        return Ok(await _ivrFactory.GetGreetingsAsync(ResolveCustomerId(customerId)));
    }

    [HttpGet("greetings/{id}")]
    public async Task<ActionResult<GreetingMessageDto>> GetGreeting(int id, [FromQuery] int? customerId = null)
    {
        var result = await _ivrFactory.GetGreetingAsync(id, ResolveCustomerId(customerId));
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("greetings")]
    public async Task<ActionResult<GreetingMessageDto>> CreateGreeting(
        [FromForm] CreateGreetingMessageRequest request,
        IFormFile? audioFile,
        [FromQuery] int? customerId = null)
    {
        var cid = ResolveCustomerId(customerId);
        string? audioFilePath = null;
        string? audioFileName = null;

        if (audioFile != null && audioFile.Length > 0)
        {
            var dir = EnsureAudioDirectory();
            audioFileName = audioFile.FileName;
            var fileName = $"greeting_{cid}_{Guid.NewGuid():N}{Path.GetExtension(audioFile.FileName)}";
            audioFilePath = Path.Combine(dir, fileName);

            await using var stream = new FileStream(audioFilePath, FileMode.Create);
            await audioFile.CopyToAsync(stream);
        }

        var result = await _ivrFactory.CreateGreetingAsync(cid, request, audioFilePath!, audioFileName);

        await AuditCrudAsync("Create", "GreetingMessage", result.Id.ToString(),
            $"Karsilama mesaji olusturuldu: {result.Name}", customerId: cid);

        return CreatedAtAction(nameof(GetGreeting), new { id = result.Id }, result);
    }

    [HttpPut("greetings/{id}")]
    public async Task<IActionResult> UpdateGreeting(int id, UpdateGreetingMessageRequest request, [FromQuery] int? customerId = null)
    {
        var cid = ResolveCustomerId(customerId);
        var (success, error) = await _ivrFactory.UpdateGreetingAsync(id, cid, request);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Update", "GreetingMessage", id.ToString(),
            $"Karsilama mesaji guncellendi: ID={id}", customerId: cid);
        return NoContent();
    }

    [HttpDelete("greetings/{id}")]
    public async Task<IActionResult> DeleteGreeting(int id, [FromQuery] int? customerId = null)
    {
        var cid = ResolveCustomerId(customerId);
        var (success, error) = await _ivrFactory.DeleteGreetingAsync(id, cid);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Delete", "GreetingMessage", id.ToString(),
            $"Karsilama mesaji silindi: ID={id}", customerId: cid);
        return NoContent();
    }

    [HttpGet("menus")]
    public async Task<ActionResult<List<IvrMenuDto>>> GetMenus([FromQuery] int? customerId = null)
    {
        return Ok(await _ivrFactory.GetIvrMenusAsync(ResolveCustomerId(customerId)));
    }

    [HttpGet("menus/{id}")]
    public async Task<ActionResult<IvrMenuDto>> GetMenu(int id, [FromQuery] int? customerId = null)
    {
        var result = await _ivrFactory.GetIvrMenuAsync(id, ResolveCustomerId(customerId));
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("menus")]
    public async Task<ActionResult<IvrMenuDto>> CreateMenu(CreateIvrMenuRequest request, [FromQuery] int? customerId = null)
    {
        var cid = ResolveCustomerId(customerId);
        var result = await _ivrFactory.CreateIvrMenuAsync(cid, request);

        await AuditCrudAsync("Create", "IvrMenu", result.Id.ToString(),
            $"IVR menu olusturuldu: {result.Name}", customerId: cid);
        return CreatedAtAction(nameof(GetMenu), new { id = result.Id }, result);
    }

    [HttpPut("menus/{id}")]
    public async Task<IActionResult> UpdateMenu(int id, UpdateIvrMenuRequest request, [FromQuery] int? customerId = null)
    {
        var cid = ResolveCustomerId(customerId);
        var (success, error) = await _ivrFactory.UpdateIvrMenuAsync(id, cid, request);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Update", "IvrMenu", id.ToString(),
            $"IVR menu guncellendi: ID={id}", customerId: cid);
        return NoContent();
    }

    [HttpDelete("menus/{id}")]
    public async Task<IActionResult> DeleteMenu(int id, [FromQuery] int? customerId = null)
    {
        var cid = ResolveCustomerId(customerId);
        var (success, error) = await _ivrFactory.DeleteIvrMenuAsync(id, cid);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Delete", "IvrMenu", id.ToString(),
            $"IVR menu silindi: ID={id}", customerId: cid);
        return NoContent();
    }

    [HttpGet("hold-music")]
    public async Task<ActionResult<List<HoldMusicDto>>> GetHoldMusics([FromQuery] int? customerId = null)
    {
        return Ok(await _ivrFactory.GetHoldMusicsAsync(ResolveCustomerId(customerId)));
    }

    [HttpPost("hold-music")]
    public async Task<ActionResult<HoldMusicDto>> CreateHoldMusic(
        [FromForm] CreateHoldMusicRequest request,
        IFormFile? audioFile,
        [FromQuery] int? customerId = null)
    {
        var cid = ResolveCustomerId(customerId);
        string? audioFilePath = null;
        string? audioFileName = null;

        if (audioFile != null && audioFile.Length > 0)
        {
            var dir = EnsureAudioDirectory();
            audioFileName = audioFile.FileName;
            var fileName = $"holdmusic_{cid}_{Guid.NewGuid():N}{Path.GetExtension(audioFile.FileName)}";
            audioFilePath = Path.Combine(dir, fileName);

            await using var stream = new FileStream(audioFilePath, FileMode.Create);
            await audioFile.CopyToAsync(stream);
        }

        var result = await _ivrFactory.CreateHoldMusicAsync(cid, request, audioFilePath!, audioFileName);

        await AuditCrudAsync("Create", "HoldMusic", result.Id.ToString(),
            $"Bekleme muzigi olusturuldu: {result.Name}", customerId: cid);
        return Ok(result);
    }

    [HttpDelete("hold-music/{id}")]
    public async Task<IActionResult> DeleteHoldMusic(int id, [FromQuery] int? customerId = null)
    {
        var cid = ResolveCustomerId(customerId);
        var (success, error) = await _ivrFactory.DeleteHoldMusicAsync(id, cid);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Delete", "HoldMusic", id.ToString(),
            $"Bekleme muzigi silindi: ID={id}", customerId: cid);
        return NoContent();
    }

    [HttpGet("business-hours")]
    public async Task<ActionResult<List<BusinessHoursDto>>> GetBusinessHours([FromQuery] int? customerId = null)
    {
        return Ok(await _ivrFactory.GetBusinessHoursAsync(ResolveCustomerId(customerId)));
    }

    [HttpPost("business-hours")]
    public async Task<IActionResult> SetBusinessHours(SetBusinessHoursRequest request, [FromQuery] int? customerId = null)
    {
        var cid = ResolveCustomerId(customerId);
        await _ivrFactory.SetBusinessHoursAsync(cid, request);

        await AuditCrudAsync("Update", "BusinessHours", null,
            "Mesai saatleri guncellendi", customerId: cid);
        return NoContent();
    }

    [HttpGet("holidays")]
    public async Task<ActionResult<List<HolidayDto>>> GetHolidays([FromQuery] int? customerId = null)
    {
        return Ok(await _ivrFactory.GetHolidaysAsync(ResolveCustomerId(customerId)));
    }

    [HttpPost("holidays")]
    public async Task<ActionResult<HolidayDto>> CreateHoliday(CreateHolidayRequest request, [FromQuery] int? customerId = null)
    {
        var cid = ResolveCustomerId(customerId);
        var result = await _ivrFactory.CreateHolidayAsync(cid, request);

        await AuditCrudAsync("Create", "Holiday", result.Id.ToString(),
            $"Tatil olusturuldu: {result.Name} ({result.Date})", customerId: cid);
        return Ok(result);
    }

    [HttpDelete("holidays/{id}")]
    public async Task<IActionResult> DeleteHoliday(int id, [FromQuery] int? customerId = null)
    {
        var cid = ResolveCustomerId(customerId);
        var (success, error) = await _ivrFactory.DeleteHolidayAsync(id, cid);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Delete", "Holiday", id.ToString(),
            $"Tatil silindi: ID={id}", customerId: cid);
        return NoContent();
    }

    [HttpGet("incoming-config")]
    public async Task<ActionResult<IncomingCallConfigDto>> GetIncomingCallConfig(
        [FromQuery] int? customerId = null, [FromQuery] int? queueId = null)
    {
        return Ok(await _ivrFactory.GetIncomingCallConfigAsync(ResolveCustomerId(customerId), queueId));
    }
}
