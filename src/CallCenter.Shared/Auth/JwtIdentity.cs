using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CallCenter.Shared.Auth;

/// <summary>
/// JWT cookie tabanli stateless kullanici kimligi. Session yerine kullanilir.
/// Çerez adı <see cref="JwtAuthCookieOptions"/> ile uygulama bazında ayarlanır.
/// </summary>
public class JwtIdentity
{
    /// <summary>Eski sabit; yeni kod <see cref="JwtAuthCookieOptions.CookieName"/> kullanır.</summary>
    public const string TokenCookie = JwtAuthCookieOptions.FallbackSharedCookieName;

    public string? Token { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token) && !IsExpired;

    public string UserName { get; init; } = "";
    public string FullName { get; init; } = "";
    public string Role { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public int CustomerRoleId { get; init; }
    public string CustomerRole { get; init; } = "";
    public bool IsCustomerAdmin { get; init; }
    public string CustomerModules { get; init; } = "";
    public int? BranchId { get; init; }

    private static string ResolveCookieName(HttpContext ctx)
    {
        var opts = ctx.RequestServices.GetService<IOptions<JwtAuthCookieOptions>>();
        var name = opts?.Value?.CookieName?.Trim();
        return string.IsNullOrEmpty(name) ? JwtAuthCookieOptions.FallbackSharedCookieName : name;
    }

    public static JwtIdentity From(HttpContext ctx)
    {
        var name = ResolveCookieName(ctx);
        var token = ctx.Request.Cookies[name];
        if (string.IsNullOrEmpty(token)
            && !string.Equals(name, JwtAuthCookieOptions.FallbackSharedCookieName, StringComparison.Ordinal))
            token = ctx.Request.Cookies[JwtAuthCookieOptions.FallbackSharedCookieName];

        if (string.IsNullOrEmpty(token)) return new JwtIdentity();
        return Parse(token);
    }

    public static JwtIdentity Parse(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return new JwtIdentity();
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
            using var doc = JsonDocument.Parse(System.Convert.FromBase64String(payload));
            var root = doc.RootElement;
            string s(string k) => root.TryGetProperty(k, out var v) ? (v.GetString() ?? "") : "";
            int i(string k) => root.TryGetProperty(k, out var v) && int.TryParse(v.GetString(), out var n) ? n : 0;
            int? io(string k) => root.TryGetProperty(k, out var v) && int.TryParse(v.GetString(), out var n) ? n : (int?)null;
            bool b(string k) => root.TryGetProperty(k, out var v) && bool.TryParse(v.GetString(), out var x) && x;
            DateTimeOffset? exp()
            {
                if (!root.TryGetProperty("exp", out var v)) return null;
                if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var unix))
                    return DateTimeOffset.FromUnixTimeSeconds(unix);
                if (long.TryParse(v.GetString(), out unix))
                    return DateTimeOffset.FromUnixTimeSeconds(unix);
                return null;
            }

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
                BranchId = io("BranchId"),
                ExpiresAt = exp()
            };
        }
        catch
        {
            return new JwtIdentity();
        }
    }
}

public static class JwtIdentityExtensions
{
    private static string ResolveCookieName(HttpContext ctx)
    {
        var opts = ctx.RequestServices.GetService<IOptions<JwtAuthCookieOptions>>();
        var name = opts?.Value?.CookieName?.Trim();
        return string.IsNullOrEmpty(name) ? JwtAuthCookieOptions.FallbackSharedCookieName : name;
    }

    private static CookieOptions CookieOpts(HttpContext ctx) => new()
    {
        HttpOnly = true,
        Secure = ctx.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = "/"
    };

    /// <summary>Razor view + controller helper. JWT cookie'sini parse edip kimlik nesnesi döner.</summary>
    public static JwtIdentity GetJwtIdentity(this HttpContext ctx) => JwtIdentity.From(ctx);

    public static void SetAuthCookie(this HttpContext ctx, string token, int days = 30)
    {
        var name = ResolveCookieName(ctx);
        var co = CookieOpts(ctx);
        co.Expires = DateTimeOffset.UtcNow.AddDays(days);

        if (!string.Equals(name, JwtAuthCookieOptions.FallbackSharedCookieName, StringComparison.Ordinal))
            ctx.Response.Cookies.Delete(JwtAuthCookieOptions.FallbackSharedCookieName, CookieOpts(ctx));

        ctx.Response.Cookies.Append(name, token, co);
    }

    public static void ClearAuthCookie(this HttpContext ctx)
    {
        var co = CookieOpts(ctx);
        var name = ResolveCookieName(ctx);
        ctx.Response.Cookies.Delete(name, co);
        if (!string.Equals(name, JwtAuthCookieOptions.FallbackSharedCookieName, StringComparison.Ordinal))
            ctx.Response.Cookies.Delete(JwtAuthCookieOptions.FallbackSharedCookieName, co);
        ctx.Response.Cookies.Delete("RememberToken", co);
    }
}
