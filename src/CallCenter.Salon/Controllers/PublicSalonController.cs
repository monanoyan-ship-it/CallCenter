using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

/// <summary>
/// Herkese acik salon profil sayfasi (auth gerekmez)
/// </summary>
public class PublicSalonController : Controller
{
    [HttpGet("salon/{slug}")]
    public IActionResult Profile(string slug)
    {
        ViewData["Slug"] = slug;
        return View();
    }

    [HttpGet("salon/{slug}/book")]
    public IActionResult Book(string slug)
    {
        ViewData["Slug"] = slug;
        return View();
    }

    [HttpGet("kesfet")]
    public IActionResult Discover()
    {
        return View();
    }

    // ─── Platform User Sayfaları ───

    [HttpGet("uye/giris")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpGet("uye/kayit")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpGet("uye/panel")]
    public IActionResult Panel()
    {
        return View();
    }
}
