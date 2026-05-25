namespace CallCenter.Shared.Security;

public enum ProxyPathSurface
{
    Crm,
    SalonAuthenticated,
    SalonPublic,
    PlatformPublic,
    Management,
    MenuPublic
}

public static class ProxyPathPolicy
{
    private static readonly string[] CrmSegments =
    [
        "crm",
        "integrations",
        "portal"
    ];

    private static readonly string[] SalonSegments =
    [
        "portal",
        "payments",
        "payment-config",
        "subscriptions"
    ];

    private static readonly string[] ManagementSegments =
    [
        "auditlogs",
        "billing",
        "cloudstorage",
        "customers",
        "kvkk",
        "management",
        "organizations",
        "payment-config",
        "payments",
        "portal",
        "service-pricing",
        "settings",
        "sln-module-requests",
        "subscriptions",
        "supervisor",
        "translations",
        "users"
    ];

    private static readonly string[] PlatformPublicSegments =
    [
        "payments",
        "platform",
        "salon"
    ];

    public static bool TryNormalize(string? path, ProxyPathSurface surface, out string normalized)
    {
        normalized = string.Empty;
        if (!TryNormalizeSyntax(path, out var candidate))
            return false;

        var allowed = surface switch
        {
            ProxyPathSurface.Crm => IsAllowedSegment(candidate, CrmSegments),
            ProxyPathSurface.SalonAuthenticated => candidate.StartsWith("sln-", StringComparison.OrdinalIgnoreCase)
                || IsAllowedSegment(candidate, SalonSegments),
            ProxyPathSurface.SalonPublic => true,
            ProxyPathSurface.PlatformPublic => IsAllowedSegment(candidate, PlatformPublicSegments),
            ProxyPathSurface.Management => IsAllowedSegment(candidate, ManagementSegments),
            ProxyPathSurface.MenuPublic => true,
            _ => false
        };

        if (!allowed)
            return false;

        normalized = candidate;
        return true;
    }

    private static bool TryNormalizeSyntax(string? path, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(path) || path.Length > 512)
            return false;

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(path.Trim());
        }
        catch (UriFormatException)
        {
            return false;
        }

        if (decoded.Contains('\\') || decoded.Contains(':') || decoded.Contains('?') || decoded.Contains('#'))
            return false;
        if (decoded.Contains("//", StringComparison.Ordinal))
            return false;

        decoded = decoded.Trim('/');
        if (decoded.Length == 0)
            return false;

        var segments = decoded.Split('/', StringSplitOptions.None);
        foreach (var segment in segments)
        {
            if (segment.Length == 0 || segment is "." or ".." || segment.Contains("..", StringComparison.Ordinal))
                return false;
            if (!segment.All(IsAllowedPathChar))
                return false;
        }

        if (segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
            return false;

        normalized = string.Join('/', segments);
        return true;
    }

    private static bool IsAllowedSegment(string normalized, IReadOnlyCollection<string> allowedSegments)
    {
        var firstSlash = normalized.IndexOf('/');
        var firstSegment = firstSlash >= 0 ? normalized[..firstSlash] : normalized;
        return allowedSegments.Any(x => x.Equals(firstSegment, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAllowedPathChar(char c)
        => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.';
}
