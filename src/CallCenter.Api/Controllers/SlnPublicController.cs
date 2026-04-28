using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

/// <summary>
/// Herkese acik salon profil endpoint'leri (auth gerekmez)
/// </summary>
[ApiController]
[Route("api/salon")]
public class SlnPublicController : ControllerBase
{
    private readonly ISlnPublicFactory _publicFactory;

    public SlnPublicController(ISlnPublicFactory publicFactory) => _publicFactory = publicFactory;

    /// <summary>Slug ile salon profili getir (branch slug veya eski profile slug)</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<SlnSalonProfileDto>> GetBySlug(string slug)
    {
        var result = await _publicFactory.GetBySlugAsync(slug);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>Tum yayinlanmis salonlari listele (merkez sube bilgileriyle)</summary>
    [HttpGet]
    public async Task<ActionResult> GetAllPublished()
    {
        var result = await _publicFactory.GetAllPublishedAsync();
        return Ok(result);
    }

    /// <summary>Discover harita için: tüm aktif şubeler (her şube ayrı marker)</summary>
    [HttpGet("branches-map")]
    public async Task<ActionResult> GetBranchesForMap()
    {
        var result = await _publicFactory.GetAllBranchesForMapAsync();
        return Ok(result);
    }

    /// <summary>Salonun onaylanmis yorumlarini getir</summary>
    [HttpGet("{slug}/reviews")]
    public async Task<ActionResult> GetReviews(string slug)
    {
        var result = await _publicFactory.GetReviewsAsync(slug);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>Salonun ekibini getir (aktif personel)</summary>
    [HttpGet("{slug}/team")]
    public async Task<ActionResult> GetTeam(string slug)
    {
        var result = await _publicFactory.GetTeamAsync(slug);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>Salonun subelerini getir (online randevu icin sube secimi)</summary>
    [HttpGet("{slug}/branches")]
    public async Task<ActionResult> GetBranches(string slug)
    {
        var result = await _publicFactory.GetBranchesAsync(slug);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>Salonun aktif uyelik planlarini getir</summary>
    [HttpGet("{slug}/memberships")]
    public async Task<ActionResult> GetMembershipPlans(string slug)
    {
        var result = await _publicFactory.GetMembershipPlansAsync(slug);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>Online uyelik basvurusu (auth gerekmez)</summary>
    [HttpPost("{slug}/membership-signup")]
    public async Task<ActionResult> MembershipSignup(string slug, [FromBody] SlnMembershipSignupDto dto)
    {
        var (success, error, result) = await _publicFactory.MembershipSignupAsync(slug, dto);
        if (!success && error == "Salon bulunamadi") return NotFound();
        return success ? Ok(result) : BadRequest(error);
    }

    /// <summary>Hizmet icin musait personelleri getir (skill eslemesi)</summary>
    [HttpGet("{slug}/available-staff")]
    public async Task<ActionResult> GetAvailableStaff(string slug, [FromQuery] int serviceId)
    {
        var result = await _publicFactory.GetAvailableStaffForServiceAsync(slug, serviceId);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>Salonun randevu politikasini getir (depozito, iptal kurallari)</summary>
    [HttpGet("{slug}/booking-policy")]
    public async Task<ActionResult> GetBookingPolicy(string slug)
    {
        var result = await _publicFactory.GetBookingPolicyAsync(slug);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>Belirli salon + tarih + hizmet icin musait saatleri getir</summary>
    [HttpGet("{slug}/available-slots")]
    public async Task<ActionResult> GetAvailableSlots(string slug, [FromQuery] int serviceId, [FromQuery] DateTime date, [FromQuery] int? personnelId = null)
    {
        var result = await _publicFactory.GetAvailableSlotsAsync(slug, serviceId, date, personnelId);
        if (result == null) return BadRequest("Hizmet bulunamadi");
        return Ok(result);
    }

    /// <summary>Online randevu olustur (auth gerekmez)</summary>
    [HttpPost("{slug}/book")]
    public async Task<ActionResult> BookAppointment(string slug, [FromBody] SlnOnlineBookingDto dto)
    {
        var buyerIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, error, result) = await _publicFactory.BookAppointmentAsync(slug, dto, buyerIp);
        if (!success && error == "Salon bulunamadi") return NotFound();
        return success ? Ok(result) : BadRequest(new { message = error });
    }

    /// <summary>Bekleme listesine kaydol (musait saat yoksa - auth gerekmez)</summary>
    [HttpPost("{slug}/waitlist")]
    public async Task<ActionResult> JoinWaitlist(string slug, [FromBody] SlnPublicWaitlistDto dto)
    {
        var (success, error, result) = await _publicFactory.JoinWaitlistAsync(slug, dto);
        if (!success && error == "Salon bulunamadi") return NotFound();
        return success ? Ok(result) : BadRequest(new { message = error });
    }
}
