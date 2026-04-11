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

        // Session dustu ama "Beni Hatirla" cookie'si varsa — session'i geri yukle
        if (string.IsNullOrEmpty(token) && controllerType != typeof(AccountController))
        {
            var rememberToken = HttpContext.Request.Cookies["RememberToken"];
            if (!string.IsNullOrEmpty(rememberToken))
            {
                // Token'dan session'i yeniden olustur
                HttpContext.Session.SetString("Token", rememberToken);
                try
                {
                    var jwtParts = rememberToken.Split('.');
                    if (jwtParts.Length == 3)
                    {
                        var jwtPayload = jwtParts[1].Replace('-', '+').Replace('_', '/');
                        switch (jwtPayload.Length % 4)
                        {
                            case 2: jwtPayload += "=="; break;
                            case 3: jwtPayload += "="; break;
                        }
                        var payloadBytes = Convert.FromBase64String(jwtPayload);
                        using var claims = System.Text.Json.JsonDocument.Parse(payloadBytes);
                        var claimRoot = claims.RootElement;

                        if (claimRoot.TryGetProperty("given_name", out var gn))
                            HttpContext.Session.SetString("UserName", gn.ToString());
                        if (claimRoot.TryGetProperty("CustomerName", out var cn))
                            HttpContext.Session.SetString("CustomerName", cn.ToString());
                        if (claimRoot.TryGetProperty("CustomerRoleId", out var cri))
                            HttpContext.Session.SetString("CustomerRoleId", cri.ToString());
                        if (claimRoot.TryGetProperty("IsCustomerAdmin", out var ica))
                            HttpContext.Session.SetString("IsCustomerAdmin", ica.ToString());
                        if (claimRoot.TryGetProperty("CustomerModules", out var cm))
                            HttpContext.Session.SetString("CustomerModules", cm.ToString());
                    }
                }
                catch { }
                token = rememberToken;
            }
            else
            {
                context.Result = RedirectToAction("Login", "Account");
                return;
            }
        }

        // Rol + modul bazli sayfa erisim kontrolu (Proxy, PublicProxy, Modules muaf)
        if (!string.IsNullOrEmpty(token)
            && controllerType != typeof(ProxyController)
            && controllerType != typeof(PublicProxyController)
            && controllerType != typeof(ModulesController))
        {
            var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "";
            var roleId = int.TryParse(HttpContext.Session.GetString("CustomerRoleId"), out var rid) ? rid : 101;

            // Rol bazli kontrol
            if (!string.IsNullOrEmpty(controllerName) && !SalonRolePermissions.CanAccess(roleId, controllerName))
            {
                context.Result = RedirectToAction("Index", "Home");
                return;
            }

            // Modul bazli kontrol
            var modulesCsv = HttpContext.Session.GetString("CustomerModules");
            if (!string.IsNullOrEmpty(modulesCsv) && !string.IsNullOrEmpty(controllerName))
            {
                var activeModuleIds = modulesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                    .Where(id => id > 0)
                    .ToHashSet();

                if (!SalonModuleControllerMap.HasModule(activeModuleIds, controllerName))
                {
                    context.Result = RedirectToAction("Index", "Home");
                    return;
                }
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
