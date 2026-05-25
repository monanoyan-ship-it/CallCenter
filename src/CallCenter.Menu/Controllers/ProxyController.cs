using CallCenter.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Menu.Controllers;

[Route("proxy/{**path}")]
public class ProxyController : MenuBaseController
{
    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    public async Task<IActionResult> Proxy(string path)
    {
        if (!ProxyPathPolicy.TryNormalize(path, ProxyPathSurface.MenuPublic, out var safePath))
            return new ObjectResult(new { message = "Proxy path not allowed." }) { StatusCode = StatusCodes.Status403Forbidden };
        if (!ProxyCsrfGuard.IsSafeOrSameOrigin(Request))
            return new ObjectResult(new { message = "Cross-site proxy request blocked." }) { StatusCode = StatusCodes.Status403Forbidden };

        using var client = CreateApiClient();
        using var request = new HttpRequestMessage(new HttpMethod(Request.Method), $"api/menu/{safePath}{Request.QueryString}");

        if (Request.ContentLength > 0 || Request.Body.CanRead && Request.Method is "POST" or "PUT" or "PATCH")
        {
            request.Content = new StreamContent(Request.Body);
            if (!string.IsNullOrWhiteSpace(Request.ContentType))
                request.Content.Headers.TryAddWithoutValidation("Content-Type", Request.ContentType);
        }

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        return new ContentResult
        {
            Content = body,
            ContentType = contentType,
            StatusCode = (int)response.StatusCode
        };
    }
}
