using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Menu.Controllers;

[Route("proxy/{**path}")]
public class ProxyController : MenuBaseController
{
    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    public async Task<IActionResult> Proxy(string path)
    {
        using var client = CreateApiClient();
        using var request = new HttpRequestMessage(new HttpMethod(Request.Method), $"api/menu/{path}{Request.QueryString}");

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
