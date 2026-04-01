using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CallCenter.Salon.Controllers;

public abstract class SlnBaseController : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var controllerType = context.Controller.GetType();
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrEmpty(token) && controllerType != typeof(AccountController))
        {
            context.Result = RedirectToAction("Login", "Account");
            return;
        }

        // Rol bazli sayfa erisim kontrolu (Proxy ve PublicProxy muaf)
        if (!string.IsNullOrEmpty(token)
            && controllerType != typeof(ProxyController)
            && controllerType != typeof(PublicProxyController))
        {
            var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "";
            var roleId = int.TryParse(HttpContext.Session.GetString("CustomerRoleId"), out var rid) ? rid : 101;

            if (!string.IsNullOrEmpty(controllerName) && !SalonRolePermissions.CanAccess(roleId, controllerName))
            {
                context.Result = RedirectToAction("Index", "Home");
                return;
            }
        }

        base.OnActionExecuting(context);
    }

    protected HttpClient CreateApiClient()
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("SalonApi");
        var token = HttpContext.Session.GetString("Token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
