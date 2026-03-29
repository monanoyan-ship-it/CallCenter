using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class ConsentFormsController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Onay Formlari";
        return View();
    }
}
