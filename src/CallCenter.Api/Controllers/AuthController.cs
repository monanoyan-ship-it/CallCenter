using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ServiceFactory _factory;

    public AuthController(ServiceFactory factory)
    {
        _factory = factory;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var svc = _factory.CreateAuthService();
        var (success, response, error) = await svc.LoginAsync(request);

        if (!success)
            return Unauthorized(new { message = error });

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh(RefreshTokenRequest request)
    {
        var svc = _factory.CreateAuthService();
        var (success, response, error) = await svc.RefreshAsync(request.RefreshToken);

        if (!success)
            return Unauthorized(new { message = error });

        return Ok(response);
    }

    [HttpPost("revoke")]
    public async Task<ActionResult> Revoke(RefreshTokenRequest request)
    {
        var svc = _factory.CreateAuthService();
        await svc.RevokeAsync(request.RefreshToken);

        return Ok(new { message = "Token iptal edildi." });
    }
}
