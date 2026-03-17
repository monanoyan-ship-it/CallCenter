using System.Text.Json;
using Azure.Core;

namespace CallCenter.Api.Services.CloudStorage;

/// <summary>
/// Refresh token ile access token alan custom TokenCredential.
/// Delegated auth modunda GraphServiceClient'a verilir.
/// </summary>
public class OneDriveRefreshTokenCredential : TokenCredential
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _refreshToken;
    private readonly string _tenantId;
    private static readonly HttpClient _http = new();
    private AccessToken? _cachedToken;

    public OneDriveRefreshTokenCredential(string clientId, string clientSecret, string refreshToken, string? tenantId)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _refreshToken = refreshToken;
        _tenantId = tenantId ?? "common";
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => GetTokenAsync(requestContext, cancellationToken).AsTask().GetAwaiter().GetResult();

    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        if (_cachedToken.HasValue && _cachedToken.Value.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
            return _cachedToken.Value;

        var tokenUrl = $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token";
        var form = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = _refreshToken,
            ["grant_type"] = "refresh_token",
            ["scope"] = "https://graph.microsoft.com/.default offline_access"
        };

        var resp = await _http.PostAsync(tokenUrl, new FormUrlEncodedContent(form), cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"OneDrive token yenileme basarisiz: {json}");

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var accessToken = root.GetProperty("access_token").GetString()!;
        var expiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600;

        _cachedToken = new AccessToken(accessToken, DateTimeOffset.UtcNow.AddSeconds(expiresIn));
        return _cachedToken.Value;
    }
}
