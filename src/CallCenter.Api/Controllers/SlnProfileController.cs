using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Api.Services;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-profile")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnProfile)]
public class SlnProfileController : ControllerBase
{
    private readonly ISlnProfileFactory _profileFactory;
    private readonly GcsUploadService _gcs;
    private readonly AppDbContext _db;

    public SlnProfileController(ISlnProfileFactory profileFactory, GcsUploadService gcs, AppDbContext db)
    {
        _profileFactory = profileFactory;
        _gcs = gcs;
        _db = db;
    }

    /// <summary>PS.5 — Salon iyzico Pazaryeri onboarding durumunu getirir.</summary>
    [HttpGet("payment-info")]
    [RequireSalonOwner]
    public async Task<ActionResult> GetPaymentInfo()
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        var profile = await _db.Set<SlnSalonProfile>().FirstOrDefaultAsync(p => p.CustomerId == cid);
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == cid);

        return Ok(new
        {
            subMerchantType = profile?.IyzicoSubMerchantType,
            iban = profile?.IyzicoIban,
            legalCompanyTitle = profile?.IyzicoLegalCompanyTitle,
            taxOffice = profile?.IyzicoTaxOffice,
            taxNumber = profile?.IyzicoTaxNumber,
            identityNumber = profile?.IyzicoIdentityNumber,
            contactName = profile?.IyzicoContactName,
            contactSurname = profile?.IyzicoContactSurname,
            onboardingStatus = profile?.IyzicoOnboardingStatus ?? 0,
            onboardedAt = profile?.IyzicoOnboardedAt,
            onboardingError = profile?.IyzicoOnboardingError,
            subMerchantKey = !string.IsNullOrEmpty(profile?.IyzicoSubMerchantKey),
            commissionPercent = customer?.MarketplaceCommissionPercent ?? 5m,
            withholdingPercent = customer?.MarketplaceWithholdingPercent ?? 0m
        });
    }

    [HttpGet]
    [RequireSalonOwner]
    public async Task<ActionResult> GetProfile()
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        var result = await _profileFactory.GetProfileAsync(cid);
        if (result == null) return Unauthorized();

        return Ok(result);
    }

    [HttpPost]
    [RequireSalonOwner]
    public async Task<ActionResult> SaveProfile([FromBody] SlnSalonProfileUpdateDto dto)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        var (success, error) = await _profileFactory.SaveProfileAsync(dto, cid);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPut("page-settings")]
    [RequireSalonOwner]
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
        if (!IsSalonOwner() && !string.Equals(type, "recipe", StringComparison.OrdinalIgnoreCase))
            return Forbid();

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
    [RequireSalonOwner]
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

    private bool IsSalonOwner()
    {
        if (User.IsInRole("Admin")) return true;
        var claim = User.FindFirst("CustomerRoleId")?.Value;
        return int.TryParse(claim, out var roleId) && roleId == SalonRoles.Ids.SalonOwner;
    }
}
