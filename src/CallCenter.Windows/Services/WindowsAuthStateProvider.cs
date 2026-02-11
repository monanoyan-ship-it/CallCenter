using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace CallCenter.Windows.Services;

/// <summary>
/// Windows uygulamasi icin JWT tabanli kimlik dogrulama durumu saglayicisi.
/// Web'deki JwtAuthStateProvider'in SecureStorage ile adapte edilmis hali.
/// </summary>
public class WindowsAuthStateProvider : AuthenticationStateProvider
{
    private readonly SecureStorage _storage;
    private const string TokenKey = "auth_token";

    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public WindowsAuthStateProvider(SecureStorage storage)
    {
        _storage = storage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _storage.GetAsync(TokenKey);

            if (string.IsNullOrWhiteSpace(token))
                return new AuthenticationState(_anonymous);

            var claims = ParseToken(token);
            if (claims == null)
            {
                // Token gecersiz veya suresi dolmus - temizle
                await _storage.RemoveAsync(TokenKey);
                return new AuthenticationState(_anonymous);
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    public void NotifyUserAuthentication(string token)
    {
        var claims = ParseToken(token);
        if (claims == null) return;

        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void NotifyUserLogout()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }

    private IEnumerable<Claim>? ParseToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            // Token suresi dolmus mu?
            if (jwt.ValidTo < DateTime.UtcNow)
                return null;

            return jwt.Claims;
        }
        catch
        {
            return null;
        }
    }
}
