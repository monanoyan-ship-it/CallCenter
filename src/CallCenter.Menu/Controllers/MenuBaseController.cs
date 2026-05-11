using System.Net.Http.Headers;
using CallCenter.Shared.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CallCenter.Menu.Controllers;

public abstract class MenuBaseController : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var controllerType = context.Controller.GetType();
        var jwt = HttpContext.GetJwtIdentity();

        if (!jwt.IsAuthenticated
            && controllerType != typeof(AccountController)
            && controllerType != typeof(HomeController)
            && controllerType != typeof(PublicMenuController))
        {
            context.Result = RedirectToAction("Login", "Account");
            return;
        }

        base.OnActionExecuting(context);
    }

    protected HttpClient CreateApiClient()
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("MenuApi");
        var token = HttpContext.GetJwtIdentity().Token;
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
