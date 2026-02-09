using CallCenter.Api.Services;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext db, TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.CustomerPersonnel)
            .FirstOrDefaultAsync(u => u.UserName == request.UserName && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı." });

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user, user.CustomerPersonnel);
        var expireMinutes = int.Parse(HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Jwt:ExpireMinutes"] ?? "480");

        return Ok(new LoginResponse
        {
            Token = token,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(expireMinutes)
        });
    }
}
