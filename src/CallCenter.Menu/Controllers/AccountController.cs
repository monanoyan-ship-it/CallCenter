using System.Text;
using System.Text.Json;
using CallCenter.Shared.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Menu.Controllers;

public class AccountController : MenuBaseController
{
    [HttpGet]
    public IActionResult Login()
    {
        var identity = HttpContext.GetJwtIdentity();
        if (identity.IsAuthenticated)
            return RedirectToAction("Index", "Dashboard");

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, bool rememberMe = false)
    {
        using var client = CreateApiClient();
        var payload = JsonSerializer.Serialize(new { username, password });
        var response = await client.PostAsync("api/menu/auth/login",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = ExtractMessage(json, "Kullanici adi veya sifre hatali.");
            ViewBag.Username = username;
            return View();
        }

        var token = ExtractToken(json);
        if (string.IsNullOrWhiteSpace(token))
        {
            ViewBag.Error = "Giris yaniti token icermiyor.";
            ViewBag.Username = username;
            return View();
        }

        HttpContext.SetAuthCookie(token, rememberMe ? 30 : 1);
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string businessName, string fullName, string email, string phone, string password)
    {
        using var client = CreateApiClient();
        var payload = JsonSerializer.Serialize(new { businessName, fullName, email, phone, password });
        var response = await client.PostAsync("api/menu/auth/register",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = ExtractMessage(json, "Kayit olusturulamadi.");
            ViewBag.BusinessName = businessName;
            ViewBag.FullName = fullName;
            ViewBag.Email = email;
            ViewBag.Phone = phone;
            return View();
        }

        ViewBag.Success = true;
        ViewBag.Message = "Hesap basvurusu alindi. Menu API hazir oldugunda panel girisi aktif olacak.";
        return View();
    }

    [HttpGet]
    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.ClearAuthCookie();
        return RedirectToAction("Login", "Account");
    }

    private static string? ExtractToken(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("token", out var token)) return token.GetString();
            if (root.TryGetProperty("accessToken", out var accessToken)) return accessToken.GetString();
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                if (data.TryGetProperty("token", out var dataToken)) return dataToken.GetString();
                if (data.TryGetProperty("accessToken", out var dataAccessToken)) return dataAccessToken.GetString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string ExtractMessage(string json, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            foreach (var key in new[] { "message", "error", "title" })
            {
                if (root.TryGetProperty(key, out var value) && value.GetString() is { Length: > 0 } text)
                    return text;
            }
        }
        catch
        {
            return fallback;
        }

        return fallback;
    }
}
