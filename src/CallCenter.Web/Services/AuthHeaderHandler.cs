using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace CallCenter.Web.Services;

/// <summary>
/// HttpClient'a JWT Bearer token ekleyen DelegatingHandler.
/// 401 Unauthorized donerse refresh token ile otomatik yenileme dener.
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IJSRuntime _js;
    private const string TokenKey = "auth_token";
    private const string RefreshTokenKey = "auth_refresh_token";

    // Sonsuz refresh dongusu engelleme
    private bool _isRefreshing;

    public AuthHeaderHandler(IJSRuntime js)
    {
        _js = js;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await AttachTokenAsync(request);

        var response = await base.SendAsync(request, cancellationToken);

        // 401 donerse ve refresh endpoint'ine gitmiyorsak, otomatik refresh dene
        if (response.StatusCode == HttpStatusCode.Unauthorized
            && !_isRefreshing
            && request.RequestUri?.AbsolutePath?.Contains("/auth/refresh") != true
            && request.RequestUri?.AbsolutePath?.Contains("/auth/login") != true)
        {
            _isRefreshing = true;
            try
            {
                var refreshed = await TryRefreshAsync(cancellationToken);
                if (refreshed)
                {
                    // Yeni token ile istegi tekrarla
                    var retryRequest = await CloneRequestAsync(request);
                    await AttachTokenAsync(retryRequest);
                    response = await base.SendAsync(retryRequest, cancellationToken);
                }
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        return response;
    }

    private async Task AttachTokenAsync(HttpRequestMessage request)
    {
        try
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch
        {
            // JSInterop hazir degil
        }
    }

    private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            var refreshToken = await _js.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh")
            {
                Content = JsonContent.Create(new { RefreshToken = refreshToken })
            };

            var response = await base.SendAsync(refreshRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return false;

            var result = await response.Content.ReadFromJsonAsync<RefreshResult>(cancellationToken);
            if (result == null) return false;

            // Yeni token'lari kaydet
            await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, result.Token);
            await _js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, result.RefreshToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        if (original.Content != null)
        {
            var content = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);
            if (original.Content.Headers.ContentType != null)
                clone.Content.Headers.ContentType = original.Content.Headers.ContentType;
        }

        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    private record RefreshResult(string Token, string RefreshToken);
}
