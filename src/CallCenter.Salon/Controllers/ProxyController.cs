using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

/// <summary>
/// Server-side API proxy. JS dosyalarinda API adresi ve token gorunmez.
/// </summary>
public class ProxyController : SlnBaseController
{
    /// <summary>
    /// SelectedBranch cookie'sini query string'e branchId olarak ekler (yoksa).
    /// Layout'taki sube dropdown'u bu cookie'yi yazar; backend filter uygulamasi icin query'e tasiriz.
    /// </summary>
    private string BuildQueryString()
    {
        var qs = Request.QueryString.ToString();
        var branchCookie = Request.Cookies["SelectedBranch"];
        if (!string.IsNullOrEmpty(branchCookie)
            && int.TryParse(branchCookie, out var bid) && bid > 0
            && !Request.Query.ContainsKey("branchId"))
        {
            qs = string.IsNullOrEmpty(qs) ? $"?branchId={bid}" : $"{qs}&branchId={bid}";
        }
        return qs;
    }

    [HttpGet("proxy/{**path}")]
    public async Task<IActionResult> Get(string path)
    {
        using var client = CreateApiClient();
        var response = await client.GetAsync($"api/{path}{BuildQueryString()}");
        return await ToJsonResult(response);
    }

    [HttpPost("proxy/{**path}")]
    public async Task<IActionResult> Post(string path)
    {
        using var client = CreateApiClient();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PostAsync($"api/{path}{Request.QueryString}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await ToJsonResult(response);
    }

    [HttpPut("proxy/{**path}")]
    public async Task<IActionResult> Put(string path)
    {
        using var client = CreateApiClient();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PutAsync($"api/{path}{Request.QueryString}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await ToJsonResult(response);
    }

    [HttpDelete("proxy/{**path}")]
    public async Task<IActionResult> Delete(string path)
    {
        using var client = CreateApiClient();
        var response = await client.DeleteAsync($"api/{path}");
        return await ToJsonResult(response);
    }

    /// <summary>
    /// API response'unu JSON ContentResult'a cevirir.
    /// Bos body'de "null" doner (jQuery dataType:'json' ile uyumlu).
    /// 204 NoContent body/Content-Length kabul etmez, StatusCodeResult doner.
    /// </summary>
    private static async Task<IActionResult> ToJsonResult(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;

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
