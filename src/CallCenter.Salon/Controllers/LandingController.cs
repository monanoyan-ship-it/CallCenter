using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

/// <summary>
/// Salon tanitim sayfasi — root URL.
/// Login olan kullanici dashboard'a yonlendirilir.
/// </summary>
public class LandingController : Controller
{
    [HttpGet("/")]
    [HttpGet("/{culture:culture}")]
    public IActionResult Index()
    {
        var token = HttpContext.Session.GetString("Token");
        if (!string.IsNullOrEmpty(token))
            return Redirect("/Home");

        return View();
    }
}
