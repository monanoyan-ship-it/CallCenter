using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

/// <summary>
/// Herkese acik salon profil sayfasi (auth gerekmez)
/// </summary>
[AllowAnonymous]
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
                ViewData["SalonFavicon"] = root.TryGetProperty("faviconUrl", out var fav) ? fav.GetString() : null;
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
            "https://sln.corplynk.com/terms",
            "https://sln.corplynk.com/data-deletion",
            "https://sln.corplynk.com/kvkk-request"
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

    [HttpGet("user/verify-email")]
    public async Task<IActionResult> VerifyEmail(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            ViewBag.Success = false;
            ViewBag.Message = "Doğrulama bağlantısı geçersiz.";
            return View();
        }

        using var client = CreateApi();
        var response = await client.GetAsync($"api/platform/verify-email?token={Uri.EscapeDataString(token)}");
        var json = await response.Content.ReadAsStringAsync();
        ViewBag.Success = response.IsSuccessStatusCode;
        ViewBag.Message = ExtractMessage(json, response.IsSuccessStatusCode
            ? "Email başarıyla doğrulandı."
            : "Doğrulama başarısız.");
        return View();
    }

    [HttpGet("user/resend-verification")]
    public IActionResult ResendVerification() => View();

    [HttpPost("user/resend-verification")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendVerification(string email)
    {
        using var client = CreateApi();
        var payload = JsonSerializer.Serialize(new { email });
        var response = await client.PostAsync("api/platform/send-verification-email",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        var json = await response.Content.ReadAsStringAsync();
        ViewBag.Success = response.IsSuccessStatusCode;
        ViewBag.Message = ExtractMessage(json, response.IsSuccessStatusCode
            ? "Doğrulama maili tekrar gönderildi."
            : "Doğrulama maili gönderilemedi.");
        ViewBag.Email = email;
        return View();
    }

    [HttpGet("user/forgot-password")]
    public IActionResult ForgotPassword() => View();

    [HttpPost("user/forgot-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        using var client = CreateApi();
        var payload = JsonSerializer.Serialize(new { email });
        await client.PostAsync("api/platform/forgot-password",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        ViewBag.Submitted = true;
        ViewBag.Message = "Eğer hesap kayıtlıysa, şifre sıfırlama bağlantısı email adresine gönderildi.";
        return View();
    }

    [HttpGet("user/reset-password")]
    public IActionResult ResetPassword(string? token)
    {
        ViewBag.Token = token ?? "";
        if (string.IsNullOrWhiteSpace(token))
            ViewBag.Error = "Sıfırlama bağlantısı geçersiz.";
        return View();
    }

    [HttpPost("user/reset-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
    {
        ViewBag.Token = token;
        if (string.IsNullOrWhiteSpace(token))
        {
            ViewBag.Error = "Sıfırlama bağlantısı geçersiz.";
            return View();
        }
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            ViewBag.Error = "Şifre en az 6 karakter olmalı.";
            return View();
        }
        if (newPassword != confirmPassword)
        {
            ViewBag.Error = "Şifreler eşleşmiyor.";
            return View();
        }

        using var client = CreateApi();
        var payload = JsonSerializer.Serialize(new { token, newPassword });
        var response = await client.PostAsync("api/platform/reset-password",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = ExtractMessage(json, "Şifre sıfırlama başarısız.");
            return View();
        }

        ViewBag.Success = true;
        ViewBag.Message = "Şifreniz başarıyla güncellendi. Giriş yapabilirsiniz.";
        return View();
    }

    private HttpClient CreateApi()
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        return factory.CreateClient("SalonApi");
    }

    private static string ExtractMessage(string json, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var m) && m.GetString() is { } msg)
                return msg;
        }
        catch { }
        return fallback;
    }
}
