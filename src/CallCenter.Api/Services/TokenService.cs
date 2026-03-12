using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.IdentityModel.Tokens;

namespace CallCenter.Api.Services;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// JWT access token olusturur.
    /// Yetki tipleri CustomerRoles.GetPermissionsForRole() ile statik olarak belirlenir.
    /// </summary>
    public string GenerateToken(User user, CustomerPersonnel? customerPersonnel = null, IEnumerable<int>? activeModuleIds = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "480");

        var roleName = UserRoles.GetById(user.RoleId)?.SystemName ?? "Agent";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.GivenName, user.FullName),
            new(ClaimTypes.Role, roleName)
        };

        // Müşteri kullanıcısıysa ek claim'ler ekle
        if (customerPersonnel != null)
        {
            claims.Add(new Claim("CustomerId", customerPersonnel.CustomerId.ToString()));
            claims.Add(new Claim("CustomerPersonnelId", customerPersonnel.Id.ToString()));
            claims.Add(new Claim("CustomerRoleId", customerPersonnel.CustomerRoleId.ToString()));

            if (customerPersonnel.IsCustomerAdmin)
                claims.Add(new Claim("IsCustomerAdmin", "true"));

            // Rol bazli statik izinler
            var permIds = string.Join(",", CustomerRoles.GetPermissionsForRole(customerPersonnel.CustomerRoleId));
            if (!string.IsNullOrEmpty(permIds))
                claims.Add(new Claim("CustomerPermissions", permIds));

            // Musteri aktif modulleri
            if (activeModuleIds != null)
            {
                var moduleIds = string.Join(",", activeModuleIds);
                if (!string.IsNullOrEmpty(moduleIds))
                    claims.Add(new Claim("CustomerModules", moduleIds));
            }
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

    /// <summary>
    /// Kriptografik olarak guvenli refresh token uretir.
    /// </summary>
    public RefreshToken GenerateRefreshToken(int userId)
    {
        var refreshExpireDays = int.Parse(_config["Jwt:RefreshExpireDays"] ?? "7");

        return new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshExpireDays),
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };
    }
}
