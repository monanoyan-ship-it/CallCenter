using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class SalesController : SlnBaseController
{
    public IActionResult Index() => View();
}
