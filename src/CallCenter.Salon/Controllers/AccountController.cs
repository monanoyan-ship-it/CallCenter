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
        return RedirectToAction("Login");
    }

    /// <summary>Abonelik odemesi sonrasi cache temizleme — JWT akisinda gerek yok ama legacy link.</summary>
    public IActionResult RefreshSession() => RedirectToAction("Index", "Home");

    private void SetAuthFromLoginResponse(string json, int days)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var token = root.GetProperty("token").GetString() ?? "";
        if (!string.IsNullOrEmpty(token))
            HttpContext.SetAuthCookie(token, days);
    }
}
