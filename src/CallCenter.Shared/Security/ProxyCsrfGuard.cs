using Microsoft.AspNetCore.Http;

namespace CallCenter.Shared.Security;

public static class ProxyCsrfGuard
{
    public static bool IsSafeOrSameOrigin(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method)
            || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method)
            || HttpMethods.IsTrace(request.Method))
            return true;

        if (string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            return true;

        var origin = request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(origin))
            return IsSameHost(request, origin);

        var referer = request.Headers.Referer.FirstOrDefault();
        return !string.IsNullOrWhiteSpace(referer) && IsSameHost(request, referer);
    }

    private static bool IsSameHost(HttpRequest request, string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return false;

        var requestHost = request.Host.Host;
        var requestPort = request.Host.Port ?? DefaultPort(request.Scheme);
        var uriPort = uri.IsDefaultPort ? DefaultPort(uri.Scheme) : uri.Port;

        return string.Equals(uri.Host, requestHost, StringComparison.OrdinalIgnoreCase)
            && requestPort == uriPort;
    }

    private static int DefaultPort(string scheme)
        => string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase) ? 443 : 80;
}
