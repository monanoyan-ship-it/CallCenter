using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Menu.Controllers;

public class PublicMenuController : Controller
{
    public IActionResult Index(string slug)
    {
        ViewBag.Slug = string.IsNullOrWhiteSpace(slug) ? "demo-kafe" : slug;
        return View();
    }
}
