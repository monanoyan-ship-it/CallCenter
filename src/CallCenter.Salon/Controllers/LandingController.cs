using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

/// <summary>
/// Salon urun tanitim sayfasi (auth gerektirmez)
/// </summary>
public class LandingController : Controller
{
    [HttpGet("/salon-app")]
    [HttpGet("/{culture}/salon-app")]
    public IActionResult Index()
    {
        return View();
    }
}
