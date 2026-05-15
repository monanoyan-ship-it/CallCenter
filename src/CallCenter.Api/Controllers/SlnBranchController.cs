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
[Route("api/sln-branches")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnBranches)]
public class SlnBranchController : ControllerBase
{
    private readonly ISlnBranchFactory _branchFactory;
    private readonly GcsUploadService _gcs;

    public SlnBranchController(ISlnBranchFactory branchFactory, GcsUploadService gcs)
    {
        _branchFactory = branchFactory;
        _gcs = gcs;
    }

    [HttpGet]
    public async Task<ActionResult<List<SlnBranchDto>>> GetBranches()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var branches = await _branchFactory.GetBranchesAsync(customerId);
        if (!IsSalonOwner())
        {
            var branchId = GetBranchId();
            if (!branchId.HasValue) return Forbid();
            branches = branches.Where(b => b.Id == branchId.Value).ToList();
        }

        return Ok(branches);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SlnBranchDto>> GetBranch(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        if (!IsSalonOwner() && GetBranchId() != id) return Forbid();

        var branch = await _branchFactory.GetBranchAsync(id, customerId);
        return branch != null ? Ok(branch) : NotFound();
    }

    [HttpPost]
    [RequireSalonOwner]
    public async Task<ActionResult<SlnBranchDto>> CreateBranch([FromBody] SlnBranchCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var branch = await _branchFactory.CreateBranchAsync(dto, customerId);
        return Ok(branch);
    }

    [HttpPut("{id}")]
    [RequireSalonOwner]
    public async Task<ActionResult> UpdateBranch(int id, [FromBody] SlnBranchUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _branchFactory.UpdateBranchAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    [RequireSalonOwner]
    public async Task<ActionResult> DeleteBranch(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _branchFactory.DeleteBranchAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    /// <summary>Firmaya ait tum subelerin city/district alanlarini TR-normalize eder</summary>
    [HttpPost("normalize-addresses")]
    [RequireSalonOwner]
    public async Task<ActionResult> NormalizeAddresses()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var result = await _branchFactory.NormalizeAddressesAsync(customerId);
        return Ok(result);
    }

    /// <summary>Sube profil/kapak/galeri gorseli yukler. Multipart form-data.</summary>
    [HttpPost("upload-image")]
    [RequireSalonOwner]
    [RequestSizeLimit(5_242_880)] // 5 MB
    public async Task<ActionResult> UploadImage(IFormFile file, [FromQuery] string type = "branch-photo")
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        if (file == null || file.Length == 0) return BadRequest("Dosya secilmedi.");
        if (file.Length > 5_242_880) return BadRequest("Dosya 5 MB'dan buyuk olamaz.");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest("Sadece JPEG, PNG ve WebP desteklenir.");

        var normalizedType = NormalizeBranchImageType(type);
        if (normalizedType == null) return BadRequest("Gecersiz gorsel tipi.");

        var ext = file.ContentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };

        var fileName = $"{normalizedType}-{Guid.NewGuid():N}{ext}";
        var path = $"salons/{customerId}/branches/{fileName}";

        using var stream = file.OpenReadStream();
        var (url, error) = await _gcs.UploadAsync(stream, path, file.ContentType);

        if (url == null) return BadRequest(error ?? "Yukleme hatasi.");
        return Ok(new { url, path });
    }

    /// <summary>WorkingHoursJson NULL olan subelere default 09:00-19:00 (Pzt-Cmt) seed eder</summary>
    [HttpPost("normalize-working-hours")]
    [RequireSalonOwner]
    public async Task<ActionResult> NormalizeWorkingHours()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var result = await _branchFactory.NormalizeWorkingHoursAsync(customerId);
        return Ok(result);
    }

    /// <summary>Lat/Lng NULL olan subelere Nominatim ile bulk geocoding (1 sn rate-limit, async).</summary>
    [HttpPost("normalize-coordinates")]
    [RequireSalonOwner]
    public async Task<ActionResult> NormalizeCoordinates()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var result = await _branchFactory.NormalizeCoordinatesAsync(customerId);
        return Ok(result);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }

    private bool IsSalonOwner()
    {
        if (User.IsInRole("Admin")) return true;
        var claim = User.FindFirst("CustomerRoleId")?.Value;
        return int.TryParse(claim, out var roleId) && roleId == SalonRoles.Ids.SalonOwner;
    }

    private static string? NormalizeBranchImageType(string? type)
    {
        return type?.Trim().ToLowerInvariant() switch
        {
            "branch-photo" => "branch-photo",
            "branch-cover" => "branch-cover",
            "branch-gallery" => "branch-gallery",
            _ => null
        };
    }
}
