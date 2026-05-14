using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

/// <summary>
/// Server-side API proxy. JS dosyalarinda API adresi ve token gorunmez.
/// </summary>
public class ProxyController : MgmtBaseController
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
            return await ToApiResult(response);
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
        var payload = JsonSerializer.Serialize(new
        {
            message = "Servise gecici olarak ulasilamadi. Lutfen daha sonra tekrar deneyin."
        });
        return Task.FromResult(new ContentResult
        {
            Content = payload,
            ContentType = "application/json; charset=utf-8",
            StatusCode = (int)HttpStatusCode.BadGateway
        });
    }

    private static async Task<IActionResult> ToApiResult(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;

        // 204 NoContent body/Content-Length kabul etmez
        if (statusCode == 204)
            return new StatusCodeResult(204);

        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
        var contentType = response.Content.Headers.ContentType?.ToString();
        var isJson = string.IsNullOrWhiteSpace(mediaType)
            || mediaType.Contains("json", StringComparison.OrdinalIgnoreCase);

        if (isJson || !response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return new ContentResult
            {
                Content = string.IsNullOrWhiteSpace(content) ? "null" : content,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/json; charset=utf-8" : contentType,
                StatusCode = statusCode
            };
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName;
        fileName = fileName?.Trim('"');

        if (string.IsNullOrWhiteSpace(contentType))
            contentType = "application/octet-stream";

        var result = new FileContentResult(bytes, contentType);
        if (!string.IsNullOrWhiteSpace(fileName))
            result.FileDownloadName = fileName;

        return result;
    }
}
