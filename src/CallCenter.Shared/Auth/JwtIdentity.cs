using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace CallCenter.Shared.Auth;

/// <summary>
/// JWT cookie tabanli stateless kullanici kimligi. Session yerine kullanilir.
/// Token cookie "AuthToken" da tutulur; tum claim ler buradan parse edilir.
/// </summary>
public class JwtIdentity
{
    public const string TokenCookie = "AuthToken";

    public string? Token { get; init; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public string UserName { get; init; } = "";
    public string FullName { get; init; } = "";
    public string Role { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public int CustomerRoleId { get; init; }
    public string CustomerRole { get; init; } = "";
    public bool IsCustomerAdmin { get; init; }
    public string CustomerModules { get; init; } = "";
    public int? BranchId { get; init; }

    public static JwtIdentity From(HttpContext ctx)
    {
        var token = ctx.Request.Cookies[TokenCookie];
        if (string.IsNullOrEmpty(token)) return new JwtIdentity();
        return Parse(token);
    }

    public static JwtIdentity Parse(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return new JwtIdentity { Token = token };
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
            using var doc = JsonDocument.Parse(System.Convert.FromBase64String(payload));
            var root = doc.RootElement;
            string s(string k) => root.TryGetProperty(k, out var v) ? (v.GetString() ?? "") : "";
            int i(string k) => root.TryGetProperty(k, out var v) && int.TryParse(v.GetString(), out var n) ? n : 0;
            int? io(string k) => root.TryGetProperty(k, out var v) && int.TryParse(v.GetString(), out var n) ? n : (int?)null;
            bool b(string k) => root.TryGetProperty(k, out var v) && bool.TryParse(v.GetString(), out var x) && x;

            // JWT standart claim isimleri (xmlsoap.org/ws/2005/05/identity/claims/...)
            const string Name = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
            const string GivenName = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname";
            const string RoleClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

            return new JwtIdentity
            {
                Token = token,
                UserName = root.TryGetProperty(Name, out var u) ? (u.GetString() ?? "") : s("name"),
                FullName = root.TryGetProperty(GivenName, out var g) ? (g.GetString() ?? "") : s("given_name"),
                Role = root.TryGetProperty(RoleClaim, out var r) ? (r.GetString() ?? "") : s("role"),
                CustomerName = s("CustomerName"),
                CustomerRole = s("CustomerRole"),
                CustomerRoleId = i("CustomerRoleId"),
                IsCustomerAdmin = b("IsCustomerAdmin"),
                CustomerModules = s("CustomerModules"),
                BranchId = io("BranchId")
            };
        }
        catch
        {
            return new JwtIdentity { Token = token };
        }
    }
}

public static class JwtIdentityExtensions
{
    /// <summary>Razor view + controller helper. JWT cookie'sini parse edip kimlik nesnesi döner.</summary>
    public static JwtIdentity GetJwtIdentity(this HttpContext ctx) => JwtIdentity.From(ctx);

    public static void SetAuthCookie(this HttpContext ctx, string token, int days = 30)
    {
        ctx.Response.Cookies.Append(JwtIdentity.TokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(days)
        });
    }

    public static void ClearAuthCookie(this HttpContext ctx)
    {
        ctx.Response.Cookies.Delete(JwtIdentity.TokenCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
        // Eski RememberToken cookie'yi de temizle (legacy)
        ctx.Response.Cookies.Delete("RememberToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }
}
