using CallCenter.Shared.Auth;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CallCenter.Salon.Controllers;

public abstract class SlnBaseController : Controller
{
    /// <summary>HttpContext.Items'da SubscriptionActive flag'i (request bazli, signed cookie cache).</summary>
    private const string SubActiveCookie = "SubActive";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var controllerType = context.Controller.GetType();
        var jwt = HttpContext.GetJwtIdentity();

        // Auth — token cookie yoksa Login'e (Account muaf)
        if (!jwt.IsAuthenticated && controllerType != typeof(AccountController))
        {
            context.Result = RedirectToAction("Login", "Account");
            return;
        }

        // Rol + modul + abonelik kontrolleri (Proxy, PublicProxy, Modules muaf)
        if (jwt.IsAuthenticated
            && controllerType != typeof(ProxyController)
            && controllerType != typeof(PublicProxyController)
            && controllerType != typeof(ModulesController))
        {
            var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "";
            var roleId = jwt.CustomerRoleId > 0 ? jwt.CustomerRoleId : 101;

            // Rol bazli
            if (!string.IsNullOrEmpty(controllerName) && !SalonRolePermissions.CanAccess(roleId, controllerName))
            {
                context.Result = RedirectToAction("Index", "Home");
                return;
            }

            // Modul bazli
            var modulesCsv = jwt.CustomerModules;
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

            // BUG2.4: Abonelik kontrolu (cookie cache 5dk TTL)
            if (controllerType != typeof(SubscriptionRequiredController)
                && controllerType != typeof(AccountController))
            {
                var subCookie = HttpContext.Request.Cookies[SubActiveCookie];
                bool? hasActive = subCookie switch { "1" => true, "0" => false, _ => (bool?)null };

                if (!hasActive.HasValue)
                {
                    hasActive = CheckSubscriptionStatus(jwt.Token!).GetAwaiter().GetResult();
                    HttpContext.Response.Cookies.Append(SubActiveCookie, hasActive.Value ? "1" : "0", new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = HttpContext.Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                        Expires = DateTimeOffset.UtcNow.AddMinutes(5)
                    });
                }

                ViewData["SubscriptionActive"] = hasActive.Value;

                if (!hasActive.Value && controllerType != typeof(HomeController))
                {
                    context.Result = RedirectToAction("Index", "SubscriptionRequired");
                    return;
                }
            }
        }

        base.OnActionExecuting(context);
    }

    private async Task<bool> CheckSubscriptionStatus(string token)
    {
        try
        {
            var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("SalonApi");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync("api/subscriptions/status");
            if (!response.IsSuccessStatusCode) return true;
            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("hasActive", out var prop) && prop.GetBoolean();
        }
        catch { return true; }
    }

    protected HttpClient CreateApiClient()
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("SalonApi");
        var token = HttpContext.GetJwtIdentity().Token;
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
