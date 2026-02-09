using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CallCenter.Shared.Entities;
using Microsoft.IdentityModel.Tokens;

namespace CallCenter.Api.Services;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(User user, CustomerPersonnel? customerPersonnel = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "480");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.GivenName, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        // Müşteri kullanıcısıysa ek claim'ler ekle
        if (customerPersonnel != null)
        {
            claims.Add(new Claim("CustomerId", customerPersonnel.CustomerId.ToString()));
            claims.Add(new Claim("CustomerPersonnelId", customerPersonnel.Id.ToString()));
            claims.Add(new Claim("CustomerPermissions", ((int)customerPersonnel.Permissions).ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
