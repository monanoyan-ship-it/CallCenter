using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Crm.Controllers;

[Route("proxy")]
public class ProxyController : CrmBaseController
{
    [HttpGet("{**path}")]
    public async Task<IActionResult> Get(string path)
    {
        using var client = CreateApiClient();
        var query = HttpContext.Request.QueryString;
        var response = await client.GetAsync($"api/{path}{query}");
        return await ToJsonResult(response);
    }

    [HttpPost("{**path}")]
    public async Task<IActionResult> Post(string path)
    {
        using var client = CreateApiClient();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PostAsync($"api/{path}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await ToJsonResult(response);
    }

    [HttpPut("{**path}")]
    public async Task<IActionResult> Put(string path)
    {
        using var client = CreateApiClient();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PutAsync($"api/{path}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await ToJsonResult(response);
    }

    [HttpDelete("{**path}")]
    public async Task<IActionResult> Delete(string path)
    {
        using var client = CreateApiClient();
        var response = await client.DeleteAsync($"api/{path}");
        return await ToJsonResult(response);
    }

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
