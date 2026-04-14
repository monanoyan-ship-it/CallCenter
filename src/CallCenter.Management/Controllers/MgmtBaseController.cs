using CallCenter.Shared.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CallCenter.Management.Controllers;

public abstract class MgmtBaseController : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var controllerType = context.Controller.GetType();
        var jwt = HttpContext.GetJwtIdentity();

        // Auth — Account ve Proxy haric tum controller'lar token gerektirir
        if (!jwt.IsAuthenticated && controllerType != typeof(AccountController))
        {
            context.Result = RedirectToAction("Login", "Account");
            return;
        }

        // Sadece Admin rolu Management paneline girebilir (Account + Proxy haric)
        if (jwt.IsAuthenticated
            && controllerType != typeof(AccountController)
            && controllerType != typeof(ProxyController))
        {
            if (!string.Equals(jwt.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.ClearAuthCookie();
                context.Result = RedirectToAction("Login", "Account");
                return;
            }
        }

        base.OnActionExecuting(context);
    }

    protected HttpClient CreateApiClient()
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("ManagementApi");
        var token = HttpContext.GetJwtIdentity().Token;
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
