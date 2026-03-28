using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class MembershipsController : SlnBaseController
{
    public IActionResult Index() => View();
}
