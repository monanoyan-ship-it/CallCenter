using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace CallCenter.Windows.Services;

/// <summary>
/// Windows uygulamasi icin kimlik dogrulama durumu saglayicisi.
/// JWT token tabanli online kimlik dogrulama.
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

            var jwtClaims = ParseToken(token);
            if (jwtClaims == null)
            {
                await _storage.RemoveAsync(TokenKey);
                return new AuthenticationState(_anonymous);
            }

            var jwtIdentity = new ClaimsIdentity(jwtClaims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(jwtIdentity));
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
