using CallCenter.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Menu.Controllers;

[Route("public-proxy/{**path}")]
public class PublicProxyController : Controller
{
    [AcceptVerbs("GET", "POST")]
    public async Task<IActionResult> Proxy(string path)
    {
        if (!ProxyPathPolicy.TryNormalize(path, ProxyPathSurface.MenuPublic, out var safePath))
            return new ObjectResult(new { message = "Proxy path not allowed." }) { StatusCode = StatusCodes.Status403Forbidden };

        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        using var client = factory.CreateClient("MenuApi");
        using var request = new HttpRequestMessage(new HttpMethod(Request.Method), $"api/menu/public/{safePath}{Request.QueryString}");

        if (Request.ContentLength > 0 || Request.Body.CanRead && Request.Method == "POST")
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
