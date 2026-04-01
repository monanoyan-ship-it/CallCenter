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
        using var client = CreateApiClient();
        var response = await client.GetAsync($"api/{path}{Request.QueryString}");
        return await ToJsonResult(response);
    }

    [HttpPost("proxy/{**path}")]
    public async Task<IActionResult> Post(string path)
    {
        using var client = CreateApiClient();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PostAsync($"api/{path}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await ToJsonResult(response);
    }

    [HttpPut("proxy/{**path}")]
    public async Task<IActionResult> Put(string path)
    {
        using var client = CreateApiClient();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PutAsync($"api/{path}",
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

    private static async Task<ContentResult> ToJsonResult(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = string.IsNullOrWhiteSpace(content) ? "null" : content,
            ContentType = "application/json; charset=utf-8",
            StatusCode = (int)response.StatusCode
        };
    }
}
