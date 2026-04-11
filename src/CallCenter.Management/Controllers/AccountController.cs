using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class AccountController : MgmtBaseController
{
    [HttpGet]
    public IActionResult Login()
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Token")))
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
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var role = root.TryGetProperty("role", out var r) ? r.GetString() : "";
        if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            ViewBag.Error = "Bu panele sadece Admin rolu ile giris yapilabilir.";
            return View();
        }

        var tokenStr = root.GetProperty("token").GetString() ?? "";
        HttpContext.Session.SetString("Token", tokenStr);
        HttpContext.Session.SetString("UserName", root.GetProperty("fullName").GetString() ?? "");
        HttpContext.Session.SetString("UserRole", role ?? "");

        if (rememberMe)
        {
            Response.Cookies.Append("RememberToken", tokenStr, new CookieOptions
            {
                HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        }

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        Response.Cookies.Delete("RememberToken");
        return RedirectToAction("Login");
    }
}
