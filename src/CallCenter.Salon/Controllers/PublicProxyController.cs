using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

/// <summary>
/// Auth gerektirmeyen public API proxy (salon profil + online randevu)
/// </summary>
public class PublicProxyController : Controller
{
    [HttpGet("proxy/salon/{**path}")]
    public async Task<IActionResult> Get(string path)
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("SalonApi");
        var response = await client.GetAsync($"api/salon/{path}{Request.QueryString}");
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = content,
            ContentType = "application/json",
            StatusCode = (int)response.StatusCode
        };
    }

    [HttpPost("proxy/salon/{**path}")]
    public async Task<IActionResult> Post(string path)
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("SalonApi");
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var response = await client.PostAsync($"api/salon/{path}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = content,
            ContentType = "application/json",
            StatusCode = (int)response.StatusCode
        };
    }
}
