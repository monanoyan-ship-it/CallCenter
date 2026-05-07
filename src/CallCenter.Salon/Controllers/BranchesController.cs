namespace CallCenter.Salon.Controllers;

public class BranchesController : SlnBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Şubeler";
        return View();
    }
}
