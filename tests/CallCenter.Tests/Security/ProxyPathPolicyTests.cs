using CallCenter.Shared.Security;

namespace CallCenter.Tests.Security;

public class ProxyPathPolicyTests
{
    [Theory]
    [InlineData("crm/contacts", ProxyPathSurface.Crm, "crm/contacts")]
    [InlineData("/sln-clients/", ProxyPathSurface.SalonAuthenticated, "sln-clients")]
    [InlineData("portal/personnel", ProxyPathSurface.SalonAuthenticated, "portal/personnel")]
    [InlineData("customers/42", ProxyPathSurface.Management, "customers/42")]
    [InlineData("platform/login", ProxyPathSurface.PlatformPublic, "platform/login")]
    [InlineData("salon/ux-kadikoy-0506013753", ProxyPathSurface.PlatformPublic, "salon/ux-kadikoy-0506013753")]
    [InlineData("branches/list", ProxyPathSurface.SalonPublic, "branches/list")]
    [InlineData("categories", ProxyPathSurface.MenuPublic, "categories")]
    public void TryNormalize_Allows_expected_proxy_paths(string path, ProxyPathSurface surface, string expected)
    {
        var allowed = ProxyPathPolicy.TryNormalize(path, surface, out var normalized);

        allowed.Should().BeTrue();
        normalized.Should().Be(expected);
    }

    [Theory]
    [InlineData("../auth/login", ProxyPathSurface.Management)]
    [InlineData("%2e%2e/auth/login", ProxyPathSurface.Management)]
    [InlineData("sln-clients/%2e%2e/auth", ProxyPathSurface.SalonAuthenticated)]
    [InlineData("http://evil.test", ProxyPathSurface.Management)]
    [InlineData("api/auth/login", ProxyPathSurface.Management)]
    [InlineData("customers//42", ProxyPathSurface.Management)]
    [InlineData("customers/42?include=all", ProxyPathSurface.Management)]
    [InlineData("customers\\42", ProxyPathSurface.Management)]
    public void TryNormalize_Rejects_escape_and_absolute_paths(string path, ProxyPathSurface surface)
    {
        var allowed = ProxyPathPolicy.TryNormalize(path, surface, out var normalized);

        allowed.Should().BeFalse();
        normalized.Should().BeEmpty();
    }

    [Theory]
    [InlineData("auth/login", ProxyPathSurface.Management)]
    [InlineData("integration/v1/health", ProxyPathSurface.Management)]
    [InlineData("users", ProxyPathSurface.Crm)]
    [InlineData("crm/contacts", ProxyPathSurface.SalonAuthenticated)]
    public void TryNormalize_Rejects_paths_outside_surface_allowlist(string path, ProxyPathSurface surface)
    {
        var allowed = ProxyPathPolicy.TryNormalize(path, surface, out var normalized);

        allowed.Should().BeFalse();
        normalized.Should().BeEmpty();
    }
}
