using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-profile")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnProfile)]
public class SlnProfileController : ControllerBase
{
    private readonly ISlnProfileFactory _profileFactory;
    private readonly GcsUploadService _gcs;

    public SlnProfileController(ISlnProfileFactory profileFactory, GcsUploadService gcs)
    {
        _profileFactory = profileFactory;
        _gcs = gcs;
    }

    [HttpGet]
    public async Task<ActionResult> GetProfile()
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        var result = await _profileFactory.GetProfileAsync(cid);
        if (result == null) return Unauthorized();

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> SaveProfile([FromBody] SlnSalonProfileUpdateDto dto)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        var (success, error) = await _profileFactory.SaveProfileAsync(dto, cid);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPut("page-settings")]
    public async Task<ActionResult> SavePageSettings([FromBody] SlnPageSettingsDto dto)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        var (success, error) = await _profileFactory.SavePageSettingsAsync(dto, cid);
        return success ? Ok() : BadRequest(error);
    }

    /// <summary>Gorsel yukle (banner, logo vb.). Multipart form-data.</summary>
    [HttpPost("upload-image")]
    [RequestSizeLimit(5_242_880)] // 5 MB
    public async Task<ActionResult> UploadImage(IFormFile file, [FromQuery] string type = "banner")
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        if (file == null || file.Length == 0) return BadRequest("Dosya secilmedi.");
        if (file.Length > 5_242_880) return BadRequest("Dosya 5 MB'dan buyuk olamaz.");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest("Sadece JPEG, PNG ve WebP desteklenir.");

        var ext = file.ContentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };

        var fileName = $"{type}-{Guid.NewGuid():N}{ext}";
        var path = $"salons/{cid}/{fileName}";

        using var stream = file.OpenReadStream();
        var (url, error) = await _gcs.UploadAsync(stream, path, file.ContentType);

        if (url == null) return BadRequest(error ?? "Yukleme hatasi.");
        return Ok(new { url, path });
    }

    /// <summary>Yuklenmis gorseli sil</summary>
    [HttpDelete("delete-image")]
    public async Task<ActionResult> DeleteImage([FromQuery] string path)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        if (string.IsNullOrEmpty(path) || !path.StartsWith($"salons/{cid}/"))
            return BadRequest("Gecersiz dosya yolu.");

        await _gcs.DeleteAsync(path);
        return Ok();
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
