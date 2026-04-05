using System.Text.Json;

namespace CallCenter.Api.Services.Email;

/// <summary>
/// Gmail OAuth2 Authorization Code Flow.
/// GoogleDriveOAuthService ile ayni pattern, farkli scope ve credential.
/// Mevcut Drive OAuth'a DOKUNMAZ.
/// </summary>
public class GmailOAuthService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;
    private static readonly HttpClient _http = new();

    // gmail.send yeterli ancak SMTP OAuth2 XOAUTH2 icin full scope gerekli
    private const string Scopes = "https://mail.google.com/ email profile";

    public GmailOAuthService(IConfiguration config)
    {
        _clientId = config["Gmail:ClientId"] ?? "";
        _clientSecret = config["Gmail:ClientSecret"] ?? "";
        _redirectUri = config["Gmail:RedirectUri"] ?? "";
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret);

    public string GetAuthorizationUrl(string state)
    {
        var redirectUri = Uri.EscapeDataString(_redirectUri);
        var scopes = Uri.EscapeDataString(Scopes);
        return $"https://accounts.google.com/o/oauth2/v2/auth" +
               $"?client_id={_clientId}&response_type=code&redirect_uri={redirectUri}" +
               $"&scope={scopes}&state={state}&access_type=offline&prompt=consent";
    }

    public async Task<GmailTokenResponse?> ExchangeCodeAsync(string code)
    {
        var form = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["code"] = code,
            ["redirect_uri"] = _redirectUri,
            ["grant_type"] = "authorization_code"
        };

        var resp = await _http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(form));
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new GmailTokenResponse
        {
            AccessToken = root.GetProperty("access_token").GetString() ?? "",
            RefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
            ExpiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600
        };
    }

    public async Task<GmailTokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        var form = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        };

        var resp = await _http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(form));
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new GmailTokenResponse
        {
            AccessToken = root.GetProperty("access_token").GetString() ?? "",
            RefreshToken = refreshToken, // refresh_token genelde donmez, eskisini koru
            ExpiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600
        };
    }

    public async Task<string?> GetUserEmailAsync(string accessToken)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/gmail/v1/users/me/profile");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("emailAddress", out var e) ? e.GetString() : null;
    }
}

public class GmailTokenResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public int ExpiresIn { get; set; }
}
