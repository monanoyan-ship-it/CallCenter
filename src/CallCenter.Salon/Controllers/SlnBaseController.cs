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

        // Session dustu ama "Beni Hatirla" cookie'si varsa — API'den yeni token al
        if (string.IsNullOrEmpty(token) && controllerType != typeof(AccountController))
        {
            var rememberToken = HttpContext.Request.Cookies["RememberToken"];
            if (!string.IsNullOrEmpty(rememberToken))
            {
                // Eski token ile refresh dene — guncel modulleri alir
                var refreshed = TryRefreshToken(rememberToken).GetAwaiter().GetResult();
                if (refreshed)
                {
                    token = HttpContext.Session.GetString("Token");
                }
                else
                {
                    // Refresh basarisiz — eski token'dan session kur (fallback)
                    RestoreSessionFromJwt(rememberToken);
                    token = rememberToken;
                }
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

    private async Task<bool> TryRefreshToken(string oldToken)
    {
        try
        {
            var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("SalonApi");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", oldToken);

            var response = await client.PostAsync("api/auth/refresh", null);
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("token", out var newTokenProp)) return false;
            var newToken = newTokenProp.GetString();
            if (string.IsNullOrEmpty(newToken)) return false;

            // Yeni token ile session kur
            HttpContext.Session.SetString("Token", newToken);
            RestoreSessionFromJwt(newToken);

            // Cookie'yi de guncelle
            HttpContext.Response.Cookies.Append("RememberToken", newToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void RestoreSessionFromJwt(string token)
    {
        try
        {
            HttpContext.Session.SetString("Token", token);
            var jwtParts = token.Split('.');
            if (jwtParts.Length != 3) return;

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
            if (claimRoot.TryGetProperty("BranchId", out var bid))
                HttpContext.Session.SetString("BranchId", bid.ToString());
        }
        catch { }
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
