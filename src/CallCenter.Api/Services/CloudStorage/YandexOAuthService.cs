using System.Text.Json;

namespace CallCenter.Api.Services.CloudStorage;

/// <summary>
/// Yandex Disk OAuth2 Authorization Code Flow yonetimi.
/// Kolay mod icin kullanilir. Client ID/Secret appsettings'ten alinir.
/// </summary>
public class YandexOAuthService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;
    private static readonly HttpClient _http = new();

    public YandexOAuthService(IConfiguration config)
    {
        _clientId = config["Yandex:ClientId"] ?? "";
        _clientSecret = config["Yandex:ClientSecret"] ?? "";
        _redirectUri = config["Yandex:RedirectUri"] ?? "";
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret);

    /// <summary>OAuth2 authorize URL olusturur.</summary>
    public string GetAuthorizationUrl(string state)
    {
        var redirectUri = Uri.EscapeDataString(_redirectUri);
        return $"https://oauth.yandex.com/authorize" +
               $"?response_type=code&client_id={_clientId}&redirect_uri={redirectUri}" +
               $"&state={state}&force_confirm=yes";
    }

    /// <summary>Authorization code ile token exchange yapar.</summary>
    public async Task<YandexTokenResponse?> ExchangeCodeAsync(string code)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret
        };

        var resp = await _http.PostAsync("https://oauth.yandex.com/token", new FormUrlEncodedContent(form));
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new YandexTokenResponse
        {
            AccessToken = root.GetProperty("access_token").GetString() ?? "",
            ExpiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt64() : 31536000
        };
    }

    /// <summary>Access token ile kullanici ve disk bilgisi alir.</summary>
    public async Task<YandexUserInfo?> GetUserInfoAsync(string accessToken)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "https://cloud-api.yandex.net/v1/disk/");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", accessToken);
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string? login = null;
        if (root.TryGetProperty("user", out var user))
            login = user.TryGetProperty("login", out var l) ? l.GetString() : null;

        return new YandexUserInfo
        {
            Login = login ?? "",
            TotalSpace = root.TryGetProperty("total_space", out var ts) ? ts.GetInt64() : 0,
            UsedSpace = root.TryGetProperty("used_space", out var us) ? us.GetInt64() : 0
        };
    }
}

public class YandexTokenResponse
{
    public string AccessToken { get; set; } = "";
    public long ExpiresIn { get; set; }
}

public class YandexUserInfo
{
    public string Login { get; set; } = "";
    public long TotalSpace { get; set; }
    public long UsedSpace { get; set; }
}
