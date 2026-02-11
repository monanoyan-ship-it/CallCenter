using System.Net.Http;

namespace CallCenter.Windows.Services;

/// <summary>
/// HttpClient'a JWT Bearer token ekleyen DelegatingHandler.
/// Her HTTP isteginde SecureStorage'dan token'i okur ve Authorization header'a ekler.
/// </summary>
public class WindowsAuthHeaderHandler : DelegatingHandler
{
    private readonly SecureStorage _storage;
    private const string TokenKey = "auth_token";

    public WindowsAuthHeaderHandler(SecureStorage storage)
    {
        _storage = storage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _storage.GetAsync(TokenKey);

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch
        {
            // Storage hazir degil - header'siz gonder
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
