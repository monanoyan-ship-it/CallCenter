using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

/// <summary>
/// Herkese acik salon profil sayfasi (auth gerekmez)
/// </summary>
public class PublicSalonController : Controller
{
    [HttpGet("salon/{slug}")]
    public async Task<IActionResult> Profile(string slug)
    {
        ViewData["Slug"] = slug;

        // SEO: Salon bilgilerini API'den al (meta tag'ler icin)
        try
        {
            var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("SalonApi");
            var response = await client.GetAsync($"api/salon/{slug}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                ViewData["SalonName"] = root.TryGetProperty("salonName", out var n) ? n.GetString() : null;
                ViewData["SalonDescription"] = root.TryGetProperty("description", out var d) ? d.GetString() : null;
                ViewData["SalonCity"] = root.TryGetProperty("city", out var c) ? c.GetString() : null;
                ViewData["SalonDistrict"] = root.TryGetProperty("district", out var di) ? di.GetString() : null;
                ViewData["SalonLogo"] = root.TryGetProperty("logoUrl", out var l) ? l.GetString() : null;
            }
        }
        catch { }

        return View();
    }

    [HttpGet("salon/{slug}/book")]
    public IActionResult Book(string slug)
    {
        ViewData["Slug"] = slug;
        return View();
    }

    [HttpGet("discover")]
    public IActionResult Discover()
    {
        return View();
    }

    // ─── Platform User Sayfaları ───

    [HttpGet("user/login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpGet("user/register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpGet("privacy")]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet("terms")]
    public IActionResult Terms()
    {
        return View();
    }

    [HttpGet("sitemap.xml")]
    [Produces("application/xml")]
    public async Task<IActionResult> Sitemap()
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("SalonApi");

        var urls = new List<string>
        {
            "https://sln.corplynk.com/discover",
            "https://sln.corplynk.com/privacy",
            "https://sln.corplynk.com/terms"
        };

        try
        {
            var response = await client.GetAsync("api/salon");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                foreach (var salon in doc.RootElement.EnumerateArray())
                {
                    if (salon.TryGetProperty("slug", out var s) && s.GetString() is string slug && !string.IsNullOrEmpty(slug))
                        urls.Add($"https://sln.corplynk.com/salon/{slug}");
                }
            }
        }
        catch { }

        var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n";
        foreach (var url in urls)
            xml += $"  <url><loc>{url}</loc></url>\n";
        xml += "</urlset>";

        return Content(xml, "application/xml");
    }

    [HttpGet("user/panel")]
    public IActionResult Panel()
    {
        return View();
    }
}
