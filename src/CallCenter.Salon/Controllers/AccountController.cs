using System.Text;
using System.Text.Json;
using CallCenter.Shared.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class AccountController : SlnBaseController
{
    [HttpGet]
    public IActionResult Login()
    {
        if (string.Equals(Request.Query["loggedOut"], "1", StringComparison.Ordinal))
            HttpContext.ClearAuthCookie();

        if (HttpContext.GetJwtIdentity().IsAuthenticated)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, bool rememberMe = false)
    {
        using var client = CreateApiClient();
        var payload = JsonSerializer.Serialize(new { username, password });
        var response = await client.PostAsync("api/auth/login",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = "Kullanici adi veya sifre hatali.";
            return View();
        }

        var json = await response.Content.ReadAsStringAsync();
        SetAuthFromLoginResponse(json, rememberMe ? 30 : 1);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.GetJwtIdentity().IsAuthenticated)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> RegisterOptions()
    {
        using var client = CreateApiClient();
        var response = await client.GetAsync("api/auth/salon-register/options");
        var json = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = json,
            ContentType = "application/json",
            StatusCode = (int)response.StatusCode
        };
    }

    [HttpPost]
    public async Task<IActionResult> DoRegister([FromBody] JsonElement body)
    {
        using var client = CreateApiClient();
        var response = await client.PostAsync("api/auth/salon-register",
            new StringContent(body.GetRawText(), Encoding.UTF8, "application/json"));

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!response.IsSuccessStatusCode || !root.TryGetProperty("success", out var s) || !s.GetBoolean())
        {
            var error = root.TryGetProperty("error", out var e) ? e.GetString() : "Kayit sirasinda hata olustu.";
            return Json(new { success = false, error });
        }

        if (root.TryGetProperty("token", out var token) && token.GetString() is string t && !string.IsNullOrEmpty(t))
        {
            SetAuthFromLoginResponse(json, 1);
        }

        return Json(new { success = true });
    }

    [HttpGet]
    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.ClearAuthCookie();
        var opt = new CookieOptions { Path = "/" };
        Response.Cookies.Delete("SlnPanelOk", opt);
        Response.Cookies.Delete("SlnSubStrict", opt);
        return RedirectToAction("Login", "Account", new { loggedOut = 1 });
    }

    /// <summary>Abonelik/modul odemesi sonrasi JWT modul claim'i ve panel cache yenileme.</summary>
    public async Task<IActionResult> RefreshSession(string? returnUrl = null)
    {
        using var client = CreateApiClient();
        var response = await client.PostAsync("api/auth/refresh-current",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            SetAuthFromLoginResponse(json, 1);
        }

        var opt = new CookieOptions { Path = "/" };
        Response.Cookies.Delete("SlnPanelOk", opt);
        Response.Cookies.Delete("SlnSubStrict", opt);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    private void SetAuthFromLoginResponse(string json, int days)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var token = root.GetProperty("token").GetString() ?? "";
        if (!string.IsNullOrEmpty(token))
            HttpContext.SetAuthCookie(token, days);
    }
}
