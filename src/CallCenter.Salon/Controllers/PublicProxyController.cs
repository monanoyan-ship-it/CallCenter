using CallCenter.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

/// <summary>
/// Auth gerektirmeyen public API proxy (salon profil + online randevu)
/// </summary>
public class PublicProxyController : Controller
{
    [HttpGet("proxy/salon")]
    public Task<IActionResult> GetSalonRoot()
        => ForwardSalonGetAsync(string.Empty);

    [HttpGet("proxy/salon/{**path}")]
    public async Task<IActionResult> Get(string path)
    {
        if (!ProxyPathPolicy.TryNormalize(path, ProxyPathSurface.SalonPublic, out var safePath)) return ForbidProxyPath();
        return await ForwardSalonGetAsync(safePath);
    }

    private async Task<IActionResult> ForwardSalonGetAsync(string safePath)
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("SalonApi");
        var apiPath = string.IsNullOrEmpty(safePath) ? "api/salon" : $"api/salon/{safePath}";
        var response = await client.GetAsync($"{apiPath}{Request.QueryString}");
        return await ProxyResultHelper.ToApiResult(response, HttpContext);
    }

    [HttpPost("proxy/salon/{**path}")]
    public async Task<IActionResult> Post(string path)
    {
        if (!ProxyPathPolicy.TryNormalize(path, ProxyPathSurface.SalonPublic, out var safePath)) return ForbidProxyPath();
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("SalonApi");
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PostAsync($"api/salon/{safePath}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await ProxyResultHelper.ToApiResult(response, HttpContext);
    }

    // ─── Platform User Proxy (auth header forward) ───

    [HttpGet("public-proxy/{**path}")]
    public async Task<IActionResult> PlatformGet(string path)
    {
        if (!ProxyPathPolicy.TryNormalize(path, ProxyPathSurface.PlatformPublic, out var safePath)) return ForbidProxyPath();
        var client = CreatePlatformClient();
        var response = await client.GetAsync($"api/{safePath}{Request.QueryString}");
        return await ProxyResultHelper.ToApiResult(response, HttpContext);
    }

    [HttpPost("public-proxy/{**path}")]
    public async Task<IActionResult> PlatformPost(string path)
    {
        if (!ProxyPathPolicy.TryNormalize(path, ProxyPathSurface.PlatformPublic, out var safePath)) return ForbidProxyPath();
        var client = CreatePlatformClient();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PostAsync($"api/{safePath}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await ProxyResultHelper.ToApiResult(response, HttpContext);
    }

    [HttpPut("public-proxy/{**path}")]
    public async Task<IActionResult> PlatformPut(string path)
    {
        if (!ProxyPathPolicy.TryNormalize(path, ProxyPathSurface.PlatformPublic, out var safePath)) return ForbidProxyPath();
        var client = CreatePlatformClient();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PutAsync($"api/{safePath}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await ProxyResultHelper.ToApiResult(response, HttpContext);
    }

    [HttpDelete("public-proxy/{**path}")]
    public async Task<IActionResult> PlatformDelete(string path)
    {
        if (!ProxyPathPolicy.TryNormalize(path, ProxyPathSurface.PlatformPublic, out var safePath)) return ForbidProxyPath();
        var client = CreatePlatformClient();
        var response = await client.DeleteAsync($"api/{safePath}");
        return await ProxyResultHelper.ToApiResult(response, HttpContext);
    }

    [HttpPost("api/payments/iyzico-callback")]
    public Task<IActionResult> IyzicoCallback()
        => ForwardPaymentPostAsync("api/payments/iyzico-callback");

    [HttpPost("api/payments/iyzico-webhook")]
    public Task<IActionResult> IyzicoWebhook()
        => ForwardPaymentPostAsync("api/payments/iyzico-webhook");

    private async Task<IActionResult> ForwardPaymentPostAsync(string apiPath)
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("SalonApi");
        using var request = new HttpRequestMessage(HttpMethod.Post, apiPath);
        request.Content = new StreamContent(Request.Body);
        if (!string.IsNullOrWhiteSpace(Request.ContentType))
            request.Content.Headers.TryAddWithoutValidation("Content-Type", Request.ContentType);

        var response = await client.SendAsync(request);
        return await ProxyResultHelper.ToApiResult(response, HttpContext);
    }

    private HttpClient CreatePlatformClient()
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("SalonApi");
        // Authorization header'i forward et (Bearer token)
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader))
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader);
        return client;
    }

    private static IActionResult ForbidProxyPath()
        => new ObjectResult(new { message = "Proxy path not allowed." }) { StatusCode = StatusCodes.Status403Forbidden };

}
