using System.Text.Json;

namespace CallCenter.Api.Services.CloudStorage;

/// <summary>
/// OneDrive OAuth2 Authorization Code Flow yonetimi.
/// Kolay ve orta mod icin kullanilir. Client ID/Secret appsettings'ten alinir.
/// </summary>
public class OneDriveOAuthService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;
    private static readonly HttpClient _http = new();

    private const string Scopes = "Files.ReadWrite.All offline_access User.Read";

    public OneDriveOAuthService(IConfiguration config)
    {
        _clientId = config["OneDrive:ClientId"] ?? "";
        _clientSecret = config["OneDrive:ClientSecret"] ?? "";
        _redirectUri = config["OneDrive:RedirectUri"] ?? "";
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret);

    /// <summary>OAuth2 authorize URL olusturur. Kullanici buraya yonlendirilir.</summary>
    public string GetAuthorizationUrl(string? tenantId, string state)
    {
        var tenant = string.IsNullOrEmpty(tenantId) ? "common" : tenantId;
        var redirectUri = Uri.EscapeDataString(_redirectUri);
        var scopes = Uri.EscapeDataString(Scopes);
        return $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize" +
               $"?client_id={_clientId}&response_type=code&redirect_uri={redirectUri}" +
               $"&scope={scopes}&state={state}&response_mode=query";
    }

    /// <summary>Authorization code ile token exchange yapar.</summary>
    public async Task<OneDriveTokenResponse?> ExchangeCodeAsync(string code, string? tenantId)
    {
        var tenant = string.IsNullOrEmpty(tenantId) ? "common" : tenantId;
        var tokenUrl = $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";

        var form = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["code"] = code,
            ["redirect_uri"] = _redirectUri,
            ["grant_type"] = "authorization_code",
            ["scope"] = Scopes
        };

        var resp = await _http.PostAsync(tokenUrl, new FormUrlEncodedContent(form));
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            return null;

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new OneDriveTokenResponse
        {
            AccessToken = root.GetProperty("access_token").GetString() ?? "",
            RefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
            ExpiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600
        };
    }

    /// <summary>Access token ile kullanicinin drive'larini listeler.</summary>
    public async Task<List<OneDriveDriveInfo>> GetDrivesAsync(string accessToken)
    {
        var drives = new List<OneDriveDriveInfo>();

        // Once /me/drive (kisisel)
        var req = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/drive");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await _http.SendAsync(req);
        if (resp.IsSuccessStatusCode)
        {
            var json = await resp.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            drives.Add(new OneDriveDriveInfo
            {
                DriveId = root.GetProperty("id").GetString() ?? "",
                Name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "OneDrive" : "OneDrive",
                DriveType = root.TryGetProperty("driveType", out var dt) ? dt.GetString() ?? "personal" : "personal",
                OwnerName = root.TryGetProperty("owner", out var ow)
                    && ow.TryGetProperty("user", out var u)
                    && u.TryGetProperty("displayName", out var dn)
                        ? dn.GetString() : null,
                TotalSpace = root.TryGetProperty("quota", out var q) && q.TryGetProperty("total", out var t) ? t.GetInt64() : 0,
                UsedSpace = root.TryGetProperty("quota", out var q2) && q2.TryGetProperty("used", out var u2) ? u2.GetInt64() : 0
            });
        }

        // SharePoint siteleri / paylasilan drive'lar
        var req2 = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/drives");
        req2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var resp2 = await _http.SendAsync(req2);
        if (resp2.IsSuccessStatusCode)
        {
            var json2 = await resp2.Content.ReadAsStringAsync();
            var doc2 = JsonDocument.Parse(json2);
            if (doc2.RootElement.TryGetProperty("value", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    var driveId = item.GetProperty("id").GetString() ?? "";
                    if (drives.Any(d => d.DriveId == driveId)) continue;
                    drives.Add(new OneDriveDriveInfo
                    {
                        DriveId = driveId,
                        Name = item.TryGetProperty("name", out var nm) ? nm.GetString() ?? "" : "",
                        DriveType = item.TryGetProperty("driveType", out var dtp) ? dtp.GetString() ?? "" : "",
                        OwnerName = item.TryGetProperty("owner", out var ow2)
                            && ow2.TryGetProperty("user", out var u3)
                            && u3.TryGetProperty("displayName", out var dn2)
                                ? dn2.GetString() : null,
                        TotalSpace = item.TryGetProperty("quota", out var q3) && q3.TryGetProperty("total", out var t2) ? t2.GetInt64() : 0,
                        UsedSpace = item.TryGetProperty("quota", out var q4) && q4.TryGetProperty("used", out var u4) ? u4.GetInt64() : 0
                    });
                }
            }
        }

        return drives;
    }

    /// <summary>Refresh token ile yeni access token alir.</summary>
    public async Task<OneDriveTokenResponse?> RefreshAccessTokenAsync(string refreshToken, string? tenantId)
    {
        var tenant = string.IsNullOrEmpty(tenantId) ? "common" : tenantId;
        var tokenUrl = $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";

        var form = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
            ["scope"] = Scopes
        };

        var resp = await _http.PostAsync(tokenUrl, new FormUrlEncodedContent(form));
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new OneDriveTokenResponse
        {
            AccessToken = root.GetProperty("access_token").GetString() ?? "",
            RefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : refreshToken,
            ExpiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600
        };
    }
}

public class OneDriveTokenResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public int ExpiresIn { get; set; }
}

public class OneDriveDriveInfo
{
    public string DriveId { get; set; } = "";
    public string Name { get; set; } = "";
    public string DriveType { get; set; } = "";
    public string? OwnerName { get; set; }
    public long TotalSpace { get; set; }
    public long UsedSpace { get; set; }
}
