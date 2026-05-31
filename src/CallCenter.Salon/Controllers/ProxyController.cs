using CallCenter.Shared.Security;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Sockets;

namespace CallCenter.Salon.Controllers;

/// <summary>
/// Server-side API proxy. JS dosyalarinda API adresi ve token gorunmez.
/// </summary>
public class ProxyController : SlnBaseController
{
    [HttpGet("proxy/{**path}")]
    public async Task<IActionResult> Get(string path)
    {
        if (!TryNormalizePath(path, out var safePath)) return ForbidProxyPath();
        using var client = CreateApiClient();
        var response = await client.GetAsync($"api/{safePath}{Request.QueryString}");
        return await ProxyResultHelper.ToApiResult(response, HttpContext);
    }

    [HttpPost("proxy/{**path}")]
    [RequestSizeLimit(20_971_520)] // 20 MB (dosya yuklemeleri ve veri importu icin)
    public async Task<IActionResult> Post(string path)
    {
        if (!TryNormalizePath(path, out var safePath)) return ForbidProxyPath();
        if (!ProxyCsrfGuard.IsSafeOrSameOrigin(Request)) return ForbidCsrf();
        try
        {
            using var client = CreateApiClient();

            // Multipart/form-data (dosya yukleme) ise binary olarak ilet
            if (Request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true)
            {
                using var multipart = new MultipartFormDataContent();
                foreach (var file in Request.Form.Files)
                {
                    var stream = file.OpenReadStream();
                    var fileContent = new StreamContent(stream);
                    if (!string.IsNullOrWhiteSpace(file.ContentType))
                        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                    multipart.Add(fileContent, file.Name, file.FileName);
                }
                // Form alanlarini da ilet
                foreach (var field in Request.Form)
                {
                    multipart.Add(new StringContent(field.Value.ToString()), field.Key);
                }
                var multipartResponse = await client.PostAsync($"api/{safePath}{Request.QueryString}", multipart);
                return await ProxyResultHelper.ToApiResult(multipartResponse, HttpContext);
            }

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var response = await client.PostAsync($"api/{safePath}{Request.QueryString}",
                new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
            return await ProxyResultHelper.ToApiResult(response, HttpContext);
        }
        catch (Exception ex) when (IsAbortedSocket(ex))
        {
            return UpstreamUnreachableResult();
        }
    }

    [HttpPut("proxy/{**path}")]
    public async Task<IActionResult> Put(string path)
    {
        if (!TryNormalizePath(path, out var safePath)) return ForbidProxyPath();
        if (!ProxyCsrfGuard.IsSafeOrSameOrigin(Request)) return ForbidCsrf();
        using var client = CreateApiClient();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PutAsync($"api/{safePath}{Request.QueryString}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await ProxyResultHelper.ToApiResult(response, HttpContext);
    }

    [HttpDelete("proxy/{**path}")]
    public async Task<IActionResult> Delete(string path)
    {
        if (!TryNormalizePath(path, out var safePath)) return ForbidProxyPath();
        if (!ProxyCsrfGuard.IsSafeOrSameOrigin(Request)) return ForbidCsrf();
        using var client = CreateApiClient();
        var response = await client.DeleteAsync($"api/{safePath}{Request.QueryString}");
        return await ProxyResultHelper.ToApiResult(response, HttpContext);
    }

    private static bool TryNormalizePath(string path, out string safePath)
        => ProxyPathPolicy.TryNormalize(path, ProxyPathSurface.SalonAuthenticated, out safePath);

    private static IActionResult ForbidProxyPath()
        => new ObjectResult(new { message = "Proxy path not allowed." }) { StatusCode = StatusCodes.Status403Forbidden };

    private static IActionResult ForbidCsrf()
        => new ObjectResult(new { message = "Cross-site proxy request blocked." }) { StatusCode = StatusCodes.Status403Forbidden };

    private static bool IsAbortedSocket(Exception ex)
        => ex is SocketException { SocketErrorCode: SocketError.OperationAborted }
            || ex is IOException { InnerException: SocketException { SocketErrorCode: SocketError.OperationAborted } }
            || ex is HttpRequestException { InnerException: IOException { InnerException: SocketException { SocketErrorCode: SocketError.OperationAborted } } };

    private static IActionResult UpstreamUnreachableResult()
        => new ObjectResult(new { message = "Servise gecici olarak ulasilamadi. Lutfen daha sonra tekrar deneyin." })
        {
            StatusCode = (int)HttpStatusCode.BadGateway
        };
}
