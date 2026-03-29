using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class ReviewsController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Yorumlar";
        return View();
    }
}
