using System.Text;
using System.Text.Json;
using CallCenter.Shared.Auth;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CallCenter.Salon.Controllers;

public abstract class SlnBaseController : Controller
{
    /// <summary>api/subscriptions/status yaniti cache (5 dk): panel erisimi / sadece aktif abonelik.</summary>
    private const string CookiePanelAccess = "SlnPanelOk";
    private const string CookieStrictSubscription = "SlnSubStrict";

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
            && controllerType != typeof(AccountController)
            && controllerType != typeof(ProxyController)
            && controllerType != typeof(PublicProxyController)
            && controllerType != typeof(ModulesController))
        {
            var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "";
            var roleId = jwt.CustomerRoleId > 0 ? jwt.CustomerRoleId : 101;

            // Rol bazli
            if (!string.IsNullOrEmpty(controllerName) && !SalonRolePermissions.CanAccess(roleId, controllerName))
            {
                if (TryRefreshCurrentSession(context, jwt.Token))
                    return;

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
                    if (TryRefreshCurrentSession(context, jwt.Token))
                        return;

                    context.Result = RedirectToAction("Index", "Home");
                    return;
                }
            }

            // Abonelik + 5 gun tahakkuk gecikmesi: panel kilidi canAccessPanel ile
            if (controllerType != typeof(SubscriptionRequiredController)
                && controllerType != typeof(AccountController))
            {
                var panelC = HttpContext.Request.Cookies[CookiePanelAccess];
                var strictC = HttpContext.Request.Cookies[CookieStrictSubscription];
                bool? canAccessPanel = panelC switch { "1" => true, "0" => false, _ => null };
                bool? hasActiveStrict = strictC switch { "1" => true, "0" => false, _ => null };

                if (!canAccessPanel.HasValue || !hasActiveStrict.HasValue)
                {
                    var access = CheckPanelAccessAsync(jwt.Token!).GetAwaiter().GetResult();
                    canAccessPanel = access.CanAccessPanel;
                    hasActiveStrict = access.HasActiveSubscription;
                    var opt = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = HttpContext.Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                        Expires = DateTimeOffset.UtcNow.AddMinutes(5)
                    };
                    HttpContext.Response.Cookies.Append(CookiePanelAccess, canAccessPanel.Value ? "1" : "0", opt);
                    HttpContext.Response.Cookies.Append(CookieStrictSubscription, hasActiveStrict.Value ? "1" : "0", opt);
                }

                ViewData["SubscriptionActive"] = hasActiveStrict!.Value;
                ViewData["CanAccessPanel"] = canAccessPanel!.Value;

                if (!canAccessPanel.Value && controllerType != typeof(HomeController))
                {
                    context.Result = RedirectToAction("Index", "SubscriptionRequired");
                    return;
                }
            }
        }

        base.OnActionExecuting(context);
    }

    private async Task<(bool CanAccessPanel, bool HasActiveSubscription)> CheckPanelAccessAsync(string token)
    {
        try
        {
            var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("SalonApi");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync("api/subscriptions/status");
            if (!response.IsSuccessStatusCode) return (true, true);

            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
                return (true, true);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            bool canAccess = true;
            if (root.TryGetProperty("canAccessPanel", out var cap))
                canAccess = cap.GetBoolean();
            else if (root.TryGetProperty("hasActive", out var haLegacy))
                canAccess = haLegacy.GetBoolean();

            bool hasActiveStrict = false;
            if (root.TryGetProperty("hasActiveSubscription", out var has))
                hasActiveStrict = has.GetBoolean();
            else if (root.TryGetProperty("hasActive", out var ha2))
                hasActiveStrict = ha2.GetBoolean();
            else
                hasActiveStrict = canAccess;

            return (canAccess, hasActiveStrict);
        }
        catch { return (true, true); }
    }

    private bool TryRefreshCurrentSession(ActionExecutingContext context, string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || Request.Query.ContainsKey("_sessionRefreshed"))
            return false;

        try
        {
            var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("SalonApi");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = client.PostAsync("api/auth/refresh-current",
                new StringContent("{}", Encoding.UTF8, "application/json")).GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
                return false;

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("token", out var tokenEl) || tokenEl.GetString() is not { Length: > 0 } newToken)
                return false;

            HttpContext.SetAuthCookie(newToken, 1);
            var opt = new CookieOptions { Path = "/" };
            Response.Cookies.Delete(CookiePanelAccess, opt);
            Response.Cookies.Delete(CookieStrictSubscription, opt);

            var returnUrl = $"{Request.Path}{Request.QueryString}";
            returnUrl += Request.QueryString.HasValue ? "&_sessionRefreshed=1" : "?_sessionRefreshed=1";
            context.Result = LocalRedirect(returnUrl);
            return true;
        }
        catch
        {
            return false;
        }
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
