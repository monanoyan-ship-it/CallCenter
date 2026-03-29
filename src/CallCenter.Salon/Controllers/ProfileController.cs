using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class ProfileController : SlnBaseController
{
    public IActionResult Index() => View();
}
