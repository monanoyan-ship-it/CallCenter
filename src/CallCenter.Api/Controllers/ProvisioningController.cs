using System.Security.Claims;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvisioningController : ControllerBase
{
    private readonly IProvisioningService _provisioning;

    public ProvisioningController(IProvisioningService provisioning) => _provisioning = provisioning;

    /// <summary>
    /// Admin: Belirli bir kullanici icin provisioning URL olusturur.
    /// Client bu URL'ye HTTP GET yaparak SIP credentials + config alir.
    /// </summary>
    [HttpPost("create")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<ActionResult<ProvisioningUrlResponse>> CreateProvisioning([FromBody] CreateProvisioningRequest req)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _provisioning.CreateProvisioningAsync(req, baseUrl);
        return Ok(result);
    }

    /// <summary>
    /// Token ile provisioning config'i getirir. AllowAnonymous — client henuz auth olmamis.
    /// One-time token: bir kez kullanildiktan sonra gecersiz olur.
    /// Zoiper/Linphone gibi SIP client'lar bu endpoint'i provisioning URL olarak kullanir.
    /// </summary>
    [HttpGet("config")]
    [AllowAnonymous]
    public async Task<ActionResult<ProvisioningConfigDto>> GetConfig([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
            return BadRequest("Token gerekli");

        var config = await _provisioning.GetProvisioningByTokenAsync(token);
        if (config == null)
            return NotFound("Gecersiz veya suresi dolmus token");

        return Ok(config);
    }

    /// <summary>
    /// Authenticated kullanici kendi provisioning config'ini alir.
    /// Windows/Mobile client ilk baslatmada bu endpoint'i kullanabilir.
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<ProvisioningConfigDto>> GetMyProvisioning()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var config = await _provisioning.GetMyProvisioningAsync(userId.Value);
        if (config == null) return NotFound();

        return Ok(config);
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim != null ? int.Parse(claim) : null;
    }
}
