using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/platform")]
public class PlatformAuthController : ControllerBase
{
    private readonly IPlatformAuthFactory _factory;

    public PlatformAuthController(IPlatformAuthFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Yeni platform kullanicisi kaydi</summary>
    [HttpPost("register")]
    public async Task<ActionResult<PlatformAuthResponse>> Register([FromBody] PlatformRegisterDto dto)
    {
        var (result, error) = await _factory.RegisterAsync(dto);
        if (result == null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>Platform kullanicisi girisi</summary>
    [HttpPost("login")]
    public async Task<ActionResult<PlatformAuthResponse>> Login([FromBody] PlatformLoginDto dto)
    {
        var (result, error) = await _factory.LoginAsync(dto);
        if (result == null) return Unauthorized(new { message = error });
        return Ok(result);
    }

    /// <summary>Mevcut kullanici bilgileri</summary>
    [HttpGet("me")]
    [Authorize(Roles = "PlatformUser")]
    public async Task<ActionResult<PlatformUserDto>> GetMe()
    {
        var user = await _factory.GetMeAsync(GetPlatformUserId());
        if (user == null) return NotFound();
        return Ok(user);
    }

    /// <summary>Profil guncelle</summary>
    [HttpPut("me")]
    [Authorize(Roles = "PlatformUser")]
    public async Task<ActionResult> UpdateMe([FromBody] PlatformUserUpdateDto dto)
    {
        var result = await _factory.UpdateMeAsync(GetPlatformUserId(), dto);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Fatura bilgilerini guncelle</summary>
    [HttpPut("billing-info")]
    [Authorize(Roles = "PlatformUser")]
    public async Task<ActionResult> UpdateBillingInfo([FromBody] PlatformBillingUpdateDto dto)
    {
        var result = await _factory.UpdateBillingInfoAsync(GetPlatformUserId(), dto);
        if (result == null) return NotFound();
        return Ok(result);
    }

    private int GetPlatformUserId()
        => int.Parse(User.FindFirstValue("PlatformUserId") ?? "0");
}
