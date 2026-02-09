using Microsoft.JSInterop;

namespace CallCenter.Web.Services;

/// <summary>
/// HttpClient'a JWT Bearer token ekleyen DelegatingHandler.
/// Her HTTP isteginde localStorage'dan token'i okur ve Authorization header'a ekler.
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IJSRuntime _js;
    private const string TokenKey = "auth_token";

    public AuthHeaderHandler(IJSRuntime js)
    {
        _js = js;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch
        {
            // JSInterop hazir degil - header'siz gonder
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
