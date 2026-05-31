using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CallCenter.Shared.Security;

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseCorpLynkSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.TryAdd("X-Content-Type-Options", "nosniff");
                headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
                headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
                headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=(self)");
                headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin-allow-popups");
                headers.TryAdd("Content-Security-Policy",
                    "default-src 'self'; " +
                    "base-uri 'self'; object-src 'none'; frame-ancestors 'self'; " +
                    "img-src 'self' data: blob: https:; " +
                    "font-src 'self' data:; " +
                    "style-src 'self' 'unsafe-inline' https://unpkg.com; " +
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://www.googletagmanager.com https://maps.googleapis.com https://static.cloudflareinsights.com https://unpkg.com; " +
                    "connect-src 'self' http://localhost:* https://localhost:* ws://localhost:* wss://localhost:* https://*.corplynk.com https://maps.googleapis.com; " +
                    "frame-src 'self' https://*.iyzipay.com https://*.iyzico.com; " +
                    "form-action 'self' https://*.iyzipay.com https://*.iyzico.com");
                return Task.CompletedTask;
            });

            await next();
        });
    }
}
