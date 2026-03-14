using System.Security.Claims;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories;

namespace CallCenter.Api.Middleware;

/// <summary>
/// API Key authentication middleware.
/// Dis sistemlerin /api/integration/v1/* endpoint'lerine erismesi icin kullanilir.
/// X-Api-Key header ile kimlik dogrulama yapar.
/// </summary>
public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeaderName = "X-Api-Key";

    public ApiKeyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Sadece /api/integration/v1/ ile baslayan isteklerde calis
        if (!context.Request.Path.StartsWithSegments("/api/integration/v1"))
        {
            await _next(context);
            return;
        }

        // API key header kontrolu
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValue) ||
            string.IsNullOrWhiteSpace(apiKeyValue))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "X-Api-Key header gerekli." });
            return;
        }

        var apiKey = apiKeyValue.ToString();
        var hash = IntegrationFactory.HashApiKey(apiKey);

        // DB'den dogrula
        using var scope = context.RequestServices.CreateScope();
        var apiKeyEs = scope.ServiceProvider.GetRequiredService<IIntegrationApiKeyEntityService>();
        var keyEntity = await apiKeyEs.GetByHashAsync(hash);

        if (keyEntity == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Gecersiz API key." });
            return;
        }

        // Expire kontrolu
        if (keyEntity.ExpiresAt.HasValue && keyEntity.ExpiresAt.Value < DateTime.UtcNow)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "API key suresi dolmus." });
            return;
        }

        // Customer aktif mi
        if (keyEntity.Customer != null && !keyEntity.Customer.IsActive)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { message = "Musteri hesabi aktif degil." });
            return;
        }

        // LastUsedAt guncelle (fire-and-forget)
        keyEntity.LastUsedAt = DateTime.UtcNow;
        apiKeyEs.Update(keyEntity);
        var uow = scope.ServiceProvider.GetRequiredService<CallCenter.Api.Infrastructure.IUnitOfWork>();
        await uow.SaveChangesAsync();

        // Claims olustur — controller'da kullanilacak
        var claims = new List<Claim>
        {
            new("ApiKeyId", keyEntity.Id.ToString()),
            new("CustomerId", keyEntity.CustomerId.ToString()),
            new("IntegrationConnectionId", keyEntity.IntegrationConnectionId.ToString()),
            new("ApiKeyScopes", keyEntity.Scopes)
        };

        var identity = new ClaimsIdentity(claims, "ApiKey");
        context.User = new ClaimsPrincipal(identity);

        await _next(context);
    }
}

/// <summary>Extension method — Program.cs'de kullanilir</summary>
public static class ApiKeyAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthMiddleware>();
    }
}
