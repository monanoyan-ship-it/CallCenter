using System.Text.Json;

namespace CallCenter.Api.Services.Email;

/// <summary>
/// Office365 / Microsoft OAuth2 Authorization Code Flow.
/// OneDriveOAuthService ile ayni pattern, SMTP.Send scope'u.
/// </summary>
public class Office365OAuthService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;
    private readonly string _tenantId;
    private static readonly HttpClient _http = new();

    private const string Scopes = "https://outlook.office365.com/SMTP.Send offline_access openid email profile";

    public Office365OAuthService(IConfiguration config)
    {
        _clientId = config["Office365Email:ClientId"] ?? "";
        _clientSecret = config["Office365Email:ClientSecret"] ?? "";
        _redirectUri = config["Office365Email:RedirectUri"] ?? "";
        _tenantId = config["Office365Email:TenantId"] ?? "common";
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret);

    public string GetAuthorizationUrl(string state)
    {
        var redirectUri = Uri.EscapeDataString(_redirectUri);
        var scopes = Uri.EscapeDataString(Scopes);
        return $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/authorize" +
               $"?client_id={_clientId}&response_type=code&redirect_uri={redirectUri}" +
               $"&scope={scopes}&state={state}&response_mode=query";
    }

    public async Task<Office365TokenResponse?> ExchangeCodeAsync(string code)
    {
        var form = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["code"] = code,
            ["redirect_uri"] = _redirectUri,
            ["grant_type"] = "authorization_code",
            ["scope"] = Scopes
        };

        var resp = await _http.PostAsync(
            $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token",
            new FormUrlEncodedContent(form));
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new Office365TokenResponse
        {
            AccessToken = root.GetProperty("access_token").GetString() ?? "",
            RefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
            ExpiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600
        };
    }

    public async Task<Office365TokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        var form = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
            ["scope"] = Scopes
        };

        var resp = await _http.PostAsync(
            $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token",
            new FormUrlEncodedContent(form));
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new Office365TokenResponse
        {
            AccessToken = root.GetProperty("access_token").GetString() ?? "",
            RefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : refreshToken,
            ExpiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600
        };
    }

    public async Task<string?> GetUserEmailAsync(string accessToken)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("mail", out var m) ? m.GetString()
             : doc.RootElement.TryGetProperty("userPrincipalName", out var u) ? u.GetString()
             : null;
    }
}

public class Office365TokenResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public int ExpiresIn { get; set; }
}
