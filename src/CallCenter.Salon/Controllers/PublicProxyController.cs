using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

/// <summary>
/// Auth gerektirmeyen public API proxy (salon profil + online randevu)
/// </summary>
public class PublicProxyController : Controller
{
    [HttpGet("proxy/salon/{**path}")]
    public async Task<IActionResult> Get(string path)
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("SalonApi");
        var response = await client.GetAsync($"api/salon/{path}{Request.QueryString}");
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = content,
            ContentType = "application/json",
            StatusCode = (int)response.StatusCode
        };
    }

    [HttpPost("proxy/salon/{**path}")]
    public async Task<IActionResult> Post(string path)
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("SalonApi");
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PostAsync($"api/salon/{path}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = content,
            ContentType = "application/json",
            StatusCode = (int)response.StatusCode
        };
    }

    // ─── Platform User Proxy (auth header forward) ───

    [HttpGet("public-proxy/{**path}")]
    public async Task<IActionResult> PlatformGet(string path)
    {
        var client = CreatePlatformClient();
        var response = await client.GetAsync($"api/{path}{Request.QueryString}");
        return await ToResult(response);
    }

    [HttpPost("public-proxy/{**path}")]
    public async Task<IActionResult> PlatformPost(string path)
    {
        var client = CreatePlatformClient();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PostAsync($"api/{path}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await ToResult(response);
    }

    [HttpPut("public-proxy/{**path}")]
    public async Task<IActionResult> PlatformPut(string path)
    {
        var client = CreatePlatformClient();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PutAsync($"api/{path}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await ToResult(response);
    }

    [HttpDelete("public-proxy/{**path}")]
    public async Task<IActionResult> PlatformDelete(string path)
    {
        var client = CreatePlatformClient();
        var response = await client.DeleteAsync($"api/{path}");
        return await ToResult(response);
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

    private static async Task<IActionResult> ToResult(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        if (statusCode == 204) return new StatusCodeResult(204);
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = string.IsNullOrWhiteSpace(content) ? "null" : content,
            ContentType = "application/json; charset=utf-8",
            StatusCode = statusCode
        };
    }
}
