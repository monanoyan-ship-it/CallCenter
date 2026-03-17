using System.Text.Json;

namespace CallCenter.Api.Services.CloudStorage;

/// <summary>
/// Google Drive OAuth2 Authorization Code Flow yonetimi.
/// Kolay mod icin kullanilir. Client ID/Secret appsettings'ten alinir.
/// </summary>
public class GoogleDriveOAuthService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;
    private static readonly HttpClient _http = new();

    private const string Scopes = "https://www.googleapis.com/auth/drive.file";

    public GoogleDriveOAuthService(IConfiguration config)
    {
        _clientId = config["GoogleDrive:ClientId"] ?? "";
        _clientSecret = config["GoogleDrive:ClientSecret"] ?? "";
        _redirectUri = config["GoogleDrive:RedirectUri"] ?? "";
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret);

    /// <summary>OAuth2 authorize URL olusturur. Kullanici buraya yonlendirilir.</summary>
    public string GetAuthorizationUrl(string state)
    {
        var redirectUri = Uri.EscapeDataString(_redirectUri);
        var scopes = Uri.EscapeDataString(Scopes);
        return $"https://accounts.google.com/o/oauth2/v2/auth" +
               $"?client_id={_clientId}&response_type=code&redirect_uri={redirectUri}" +
               $"&scope={scopes}&state={state}&access_type=offline&prompt=consent";
    }

    /// <summary>Authorization code ile token exchange yapar.</summary>
    public async Task<GoogleDriveTokenResponse?> ExchangeCodeAsync(string code)
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

        return new GoogleDriveTokenResponse
        {
            AccessToken = root.GetProperty("access_token").GetString() ?? "",
            RefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
            ExpiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600
        };
    }

    /// <summary>Access token ile kullanici bilgisi ve disk kotasi alir.</summary>
    public async Task<GoogleDriveUserInfo?> GetUserInfoAsync(string accessToken)
    {
        var req = new HttpRequestMessage(HttpMethod.Get,
            "https://www.googleapis.com/drive/v3/about?fields=user,storageQuota");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string? email = null, displayName = null;
        if (root.TryGetProperty("user", out var user))
        {
            email = user.TryGetProperty("emailAddress", out var e) ? e.GetString() : null;
            displayName = user.TryGetProperty("displayName", out var d) ? d.GetString() : null;
        }

        long totalSpace = 0, usedSpace = 0;
        if (root.TryGetProperty("storageQuota", out var quota))
        {
            if (quota.TryGetProperty("limit", out var l)) long.TryParse(l.GetString(), out totalSpace);
            if (quota.TryGetProperty("usage", out var u)) long.TryParse(u.GetString(), out usedSpace);
        }

        return new GoogleDriveUserInfo
        {
            Email = email ?? "",
            DisplayName = displayName ?? "",
            TotalSpace = totalSpace,
            UsedSpace = usedSpace
        };
    }

    /// <summary>Root seviye klasorleri listeler (klasor secimi icin).</summary>
    public async Task<List<GoogleDriveFolderInfo>> GetRootFoldersAsync(string accessToken)
    {
        var folders = new List<GoogleDriveFolderInfo>();

        var req = new HttpRequestMessage(HttpMethod.Get,
            "https://www.googleapis.com/drive/v3/files?q=mimeType%3D%27application/vnd.google-apps.folder%27+and+%27root%27+in+parents+and+trashed%3Dfalse&fields=files(id,name)&pageSize=50");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return folders;

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("files", out var files))
        {
            foreach (var file in files.EnumerateArray())
            {
                folders.Add(new GoogleDriveFolderInfo
                {
                    Id = file.GetProperty("id").GetString() ?? "",
                    Name = file.GetProperty("name").GetString() ?? ""
                });
            }
        }

        return folders;
    }
}

public class GoogleDriveTokenResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public int ExpiresIn { get; set; }
}

public class GoogleDriveUserInfo
{
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public long TotalSpace { get; set; }
    public long UsedSpace { get; set; }
}

public class GoogleDriveFolderInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}
