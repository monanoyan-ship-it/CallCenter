using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class RecipesController : SlnBaseController
{
    public IActionResult Index() => View();
}
