using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

/// <summary>
/// Server-side API proxy. JS dosyalarinda API adresi ve token gorunmez.
/// </summary>
public class ProxyController : SlnBaseController
{
    [HttpGet("proxy/{**path}")]
    public async Task<IActionResult> Get(string path)
    {
        using var client = CreateApiClient();
        var response = await client.GetAsync($"api/{path}{Request.QueryString}");
        return await ToApiResult(response);
    }

    [HttpPost("proxy/{**path}")]
    [RequestSizeLimit(10_485_760)] // 10 MB (dosya yuklemeleri icin)
    public async Task<IActionResult> Post(string path)
    {
        using var client = CreateApiClient();

        // Multipart/form-data (dosya yukleme) ise binary olarak ilet
        if (Request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true)
        {
            var multipart = new MultipartFormDataContent();
            foreach (var file in Request.Form.Files)
            {
                var stream = file.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                multipart.Add(fileContent, file.Name, file.FileName);
            }
            // Form alanlarini da ilet
            foreach (var field in Request.Form)
            {
                multipart.Add(new StringContent(field.Value!), field.Key);
            }
            var multipartResponse = await client.PostAsync($"api/{path}{Request.QueryString}", multipart);
            return await ToApiResult(multipartResponse);
        }

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PostAsync($"api/{path}{Request.QueryString}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await ToApiResult(response);
    }

    [HttpPut("proxy/{**path}")]
    public async Task<IActionResult> Put(string path)
    {
        using var client = CreateApiClient();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PutAsync($"api/{path}{Request.QueryString}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await ToApiResult(response);
    }

    [HttpDelete("proxy/{**path}")]
    public async Task<IActionResult> Delete(string path)
    {
        using var client = CreateApiClient();
        var response = await client.DeleteAsync($"api/{path}");
        return await ToApiResult(response);
    }

    /// <summary>
    /// API response'unu JSON veya dosya cevabina cevirir.
    /// Bos body'de "null" doner (jQuery dataType:'json' ile uyumlu).
    /// 204 NoContent body/Content-Length kabul etmez, StatusCodeResult doner.
    /// </summary>
    private static async Task<IActionResult> ToApiResult(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;

        if (statusCode == 204)
            return new StatusCodeResult(204);

        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
        var contentType = response.Content.Headers.ContentType?.ToString();
        var isJson = string.IsNullOrWhiteSpace(mediaType)
            || mediaType.Contains("json", StringComparison.OrdinalIgnoreCase);

        if (isJson || !response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            return new ContentResult
            {
                Content = string.IsNullOrWhiteSpace(text) ? "null" : text,
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
