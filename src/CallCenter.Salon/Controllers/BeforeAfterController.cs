using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class BeforeAfterController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Once/Sonra Fotograflari";
        return View();
    }
}
