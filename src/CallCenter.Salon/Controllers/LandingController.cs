using CallCenter.Shared.Auth;
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
        if (HttpContext.GetJwtIdentity().IsAuthenticated)
            return Redirect("/Home");
        return View();
    }
}
