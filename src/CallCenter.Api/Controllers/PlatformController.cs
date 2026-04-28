using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

/// <summary>
/// Platform son kullanici API'si.
/// Salon uyelik, randevu, profil, sadakat islemleri.
/// </summary>
[ApiController]
[Route("api/platform")]
[Authorize(Roles = "PlatformUser")]
public class PlatformController : ControllerBase
{
    private readonly IPlatformFactory _factory;

    public PlatformController(IPlatformFactory factory) => _factory = factory;

    // ═══ SALON ÜYELİK ═══

    /// <summary>Üye olduğum salonlar</summary>
    [HttpGet("salons")]
    public async Task<ActionResult<List<PlatformSalonDto>>> GetMySalons()
    {
        return Ok(await _factory.GetMySalonsAsync(GetPlatformUserId()));
    }

    /// <summary>Salona üye ol</summary>
    [HttpPost("salons/join")]
    public async Task<ActionResult> JoinSalon([FromBody] PlatformJoinSalonDto dto)
    {
        var (success, error) = await _factory.JoinSalonAsync(GetPlatformUserId(), dto.CustomerId);
        if (!success)
        {
            if (error == null) return Unauthorized();
            return BadRequest(new { message = error });
        }
        return Ok(new { message = "Salona üye oldunuz." });
    }

    /// <summary>Salon üyeliğinden ayrıl</summary>
    [HttpDelete("salons/{customerId}")]
    public async Task<ActionResult> LeaveSalon(int customerId)
    {
        var (success, error) = await _factory.LeaveSalonAsync(GetPlatformUserId(), customerId);
        if (!success) return NotFound();
        return Ok(new { message = "Salon üyeliğiniz sonlandırıldı." });
    }

    /// <summary>Favori toggle</summary>
    [HttpPost("salons/{customerId}/favorite")]
    public async Task<ActionResult> ToggleFavorite(int customerId)
    {
        var (success, isFavorite) = await _factory.ToggleFavoriteAsync(GetPlatformUserId(), customerId);
        if (!success) return NotFound();
        return Ok(new { isFavorite });
    }

    // ═══ RANDEVU ═══

    /// <summary>Randevularım (gelecek + geçmiş)</summary>
    [HttpGet("appointments")]
    public async Task<ActionResult<List<PlatformAppointmentDto>>> GetMyAppointments([FromQuery] bool past = false)
    {
        return Ok(await _factory.GetMyAppointmentsAsync(GetPlatformUserId(), past));
    }

    /// <summary>Randevu oluştur</summary>
    [HttpPost("appointments")]
    public async Task<ActionResult> CreateAppointment([FromBody] PlatformCreateAppointmentDto dto)
    {
        var (appointmentId, error) = await _factory.CreateAppointmentAsync(GetPlatformUserId(), dto);
        if (appointmentId == null) return BadRequest(new { message = error });
        return Ok(new { appointmentId, message = "Randevu oluşturuldu." });
    }

    /// <summary>Randevu iptal (depozito varsa politikaya gore iade/kesinti yapar)</summary>
    [HttpDelete("appointments/{id}")]
    public async Task<ActionResult> CancelAppointment(int id)
    {
        var (success, error, message) = await _factory.CancelAppointmentAsync(GetPlatformUserId(), id);
        if (!success)
        {
            if (error == "Randevu bulunamadı.") return NotFound();
            return BadRequest(new { message = error });
        }
        return Ok(new { message });
    }

    // ═══ SADAKAt ═══

    /// <summary>Sadakat bilgilerim (salon bazlı)</summary>
    [HttpGet("loyalty")]
    public async Task<ActionResult<List<PlatformLoyaltyDto>>> GetMyLoyalty()
    {
        return Ok(await _factory.GetMyLoyaltyAsync(GetPlatformUserId()));
    }

    // ═══ SALON KEŞFET ═══

    /// <summary>Yayında olan salonları listele</summary>
    [HttpGet("discover")]
    [AllowAnonymous]
    public async Task<ActionResult> DiscoverSalons([FromQuery] string? city, [FromQuery] string? search, [FromQuery] int page = 1)
    {
        return Ok(await _factory.DiscoverSalonsAsync(city, search, page));
    }

    // ═══ HELPERS ═══

    private int GetPlatformUserId()
        => int.Parse(User.FindFirstValue("PlatformUserId") ?? "0");
}
