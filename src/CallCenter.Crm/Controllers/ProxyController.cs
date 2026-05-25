using CallCenter.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Crm.Controllers;

[Route("proxy")]
public class ProxyController : CrmBaseController
{
    [HttpGet("{**path}")]
    public async Task<IActionResult> Get(string path)
    {
        if (!TryNormalizePath(path, out var safePath)) return ForbidProxyPath();
        using var client = CreateApiClient();
        var query = HttpContext.Request.QueryString;
        var response = await client.GetAsync($"api/{safePath}{query}");
        return await ToJsonResult(response);
    }

    [HttpPost("{**path}")]
    public async Task<IActionResult> Post(string path)
    {
        if (!TryNormalizePath(path, out var safePath)) return ForbidProxyPath();
        if (!ProxyCsrfGuard.IsSafeOrSameOrigin(Request)) return ForbidCsrf();
        using var client = CreateApiClient();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PostAsync($"api/{safePath}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await ToJsonResult(response);
    }

    [HttpPut("{**path}")]
    public async Task<IActionResult> Put(string path)
    {
        if (!TryNormalizePath(path, out var safePath)) return ForbidProxyPath();
        if (!ProxyCsrfGuard.IsSafeOrSameOrigin(Request)) return ForbidCsrf();
        using var client = CreateApiClient();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PutAsync($"api/{safePath}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await ToJsonResult(response);
    }

    [HttpDelete("{**path}")]
    public async Task<IActionResult> Delete(string path)
    {
        if (!TryNormalizePath(path, out var safePath)) return ForbidProxyPath();
        if (!ProxyCsrfGuard.IsSafeOrSameOrigin(Request)) return ForbidCsrf();
        using var client = CreateApiClient();
        var response = await client.DeleteAsync($"api/{safePath}");
        return await ToJsonResult(response);
    }

    private static bool TryNormalizePath(string path, out string safePath)
        => ProxyPathPolicy.TryNormalize(path, ProxyPathSurface.Crm, out safePath);

    private static IActionResult ForbidProxyPath()
        => new ObjectResult(new { message = "Proxy path not allowed." }) { StatusCode = StatusCodes.Status403Forbidden };

    private static IActionResult ForbidCsrf()
        => new ObjectResult(new { message = "Cross-site proxy request blocked." }) { StatusCode = StatusCodes.Status403Forbidden };

    private static async Task<IActionResult> ToJsonResult(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;

        if (statusCode == 204)
            return new StatusCodeResult(204);

        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            StatusCode = statusCode,
            Content = content,
            ContentType = "application/json"
        };
    }
}
