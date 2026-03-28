using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class PackagesController : SlnBaseController
{
    public IActionResult Index() => View();
}
