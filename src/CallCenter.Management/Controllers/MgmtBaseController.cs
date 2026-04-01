using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CallCenter.Management.Controllers;

public abstract class MgmtBaseController : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var controllerType = context.Controller.GetType();
        var token = HttpContext.Session.GetString("Token");

        // Login sayfasi ve Proxy haric tum controller'lar auth gerektirir
        if (string.IsNullOrEmpty(token)
            && controllerType != typeof(AccountController))
        {
            context.Result = RedirectToAction("Login", "Account");
            return;
        }

        // Sadece Admin rolu girebilir
        if (!string.IsNullOrEmpty(token)
            && controllerType != typeof(AccountController)
            && controllerType != typeof(ProxyController))
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "";
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Session.Clear();
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
        var token = HttpContext.Session.GetString("Token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
