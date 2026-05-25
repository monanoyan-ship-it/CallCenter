using CallCenter.Shared.Security;
using Microsoft.AspNetCore.Http;

namespace CallCenter.Tests.Security;

public class ProxyCsrfGuardTests
{
    [Fact]
    public void IsSafeOrSameOrigin_AllowsSameOriginPost()
    {
        var context = CreateContext("POST", "sln.corplynk.com", "https://sln.corplynk.com");

        ProxyCsrfGuard.IsSafeOrSameOrigin(context.Request).Should().BeTrue();
    }

    [Fact]
    public void IsSafeOrSameOrigin_BlocksCrossOriginPost()
    {
        var context = CreateContext("POST", "sln.corplynk.com", "https://evil.example");

        ProxyCsrfGuard.IsSafeOrSameOrigin(context.Request).Should().BeFalse();
    }

    [Fact]
    public void IsSafeOrSameOrigin_AllowsGetWithoutOrigin()
    {
        var context = CreateContext("GET", "sln.corplynk.com", null);

        ProxyCsrfGuard.IsSafeOrSameOrigin(context.Request).Should().BeTrue();
    }

    private static DefaultHttpContext CreateContext(string method, string host, string? origin)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Scheme = "https";
        context.Request.Host = new HostString(host);
        if (origin != null)
            context.Request.Headers.Origin = origin;
        return context;
    }
}
