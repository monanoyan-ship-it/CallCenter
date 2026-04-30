using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

/// <summary>
/// Server-side API proxy. JS dosyalarinda API adresi ve token gorunmez.
/// </summary>
public class ProxyController(IConfiguration config) : MgmtBaseController
{
    [HttpGet("proxy/{**path}")]
    public async Task<IActionResult> Get(string path)
    {
        return await ForwardAsync(client => client.GetAsync($"api/{path}{Request.QueryString}"));
    }

    [HttpPost("proxy/{**path}")]
    public async Task<IActionResult> Post(string path)
    {
        return await ForwardAsync(async client =>
        {
            if (Request.HasFormContentType && Request.Form.Files.Count > 0)
            {
                var content = new MultipartFormDataContent();
                foreach (var file in Request.Form.Files)
                {
                    var stream = file.OpenReadStream();
                    content.Add(new StreamContent(stream), file.Name, file.FileName);
                }
                return await client.PostAsync($"api/{path}{Request.QueryString}", content);
            }

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            return await client.PostAsync($"api/{path}{Request.QueryString}",
                new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        });
    }

    [HttpPut("proxy/{**path}")]
    public async Task<IActionResult> Put(string path)
    {
        return await ForwardAsync(async client =>
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            return await client.PutAsync($"api/{path}{Request.QueryString}",
                new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        });
    }

    [HttpDelete("proxy/{**path}")]
    public async Task<IActionResult> Delete(string path)
    {
        return await ForwardAsync(client => client.DeleteAsync($"api/{path}"));
    }

    private async Task<IActionResult> ForwardAsync(Func<HttpClient, Task<HttpResponseMessage>> send)
    {
        try
        {
            using var client = CreateApiClient();
            var response = await send(client);
            return await ToJsonResult(response);
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionRefused })
        {
            return await UpstreamUnreachableResult();
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
        {
            return await UpstreamUnreachableResult();
        }
    }

    private Task<ContentResult> UpstreamUnreachableResult()
    {
        var apiUrl = config["ApiBaseUrl"] ?? "(yok)";
        var payload = JsonSerializer.Serialize(new
        {
            message = "API'ye baglanilamadi (baglanti reddedildi). CallCenter.Api calisiyor mu kontrol edin.",
            apiBaseUrl = apiUrl,
            hint = "Development: Api projesini http://localhost:5041 uzerinde calistirin; veya appsettings.Development.json icindeki ApiBaseUrl degerini guncelleyin."
        });
        return Task.FromResult(new ContentResult
        {
            Content = payload,
            ContentType = "application/json; charset=utf-8",
            StatusCode = (int)HttpStatusCode.BadGateway
        });
    }

    private static async Task<IActionResult> ToJsonResult(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;

        // 204 NoContent body/Content-Length kabul etmez
        if (statusCode == 204)
            return new StatusCodeResult(204);

        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = string.IsNullOrWhiteSpace(content) ? "null" : content,
            ContentType = "application/json; charset=utf-8",
            StatusCode = statusCode
        };
    }
}
