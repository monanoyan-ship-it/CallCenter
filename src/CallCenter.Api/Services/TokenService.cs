using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
    /// JWT token olusturur.
    /// activePermissionTypeIds: Musteri kullanicisi icin aktif yetki tipi ID'leri (virgul ile ayrilmis claim)
    /// </summary>
    public string GenerateToken(User user, CustomerPersonnel? customerPersonnel = null, IEnumerable<int>? activePermissionTypeIds = null)
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

            // Musteri admin'i ise ozel claim ekle — tum izinlere sahip
            if (customerPersonnel.IsCustomerAdmin)
            {
                claims.Add(new Claim("IsCustomerAdmin", "true"));

                // Tum izin ID'lerini claim'e yaz (NavMenu/PermissionService icin)
                var allPermIds = string.Join(",", CustomerPermissionTypes.All.Select(p => p.Id));
                claims.Add(new Claim("CustomerPermissions", allPermIds));
            }
            else if (activePermissionTypeIds != null)
            {
                // Normal kullanici: Aktif yetki tipi ID'leri virgülle ayrılmış string olarak
                var permissionIds = string.Join(",", activePermissionTypeIds);
                claims.Add(new Claim("CustomerPermissions", permissionIds));
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
}
