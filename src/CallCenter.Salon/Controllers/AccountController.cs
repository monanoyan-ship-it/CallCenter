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

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = ExtractMessage(json, "Kullanıcı adı veya şifre hatalı.");
            ViewBag.Username = username;
            return View();
        }

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

        var emailVerificationRequired = root.TryGetProperty("emailVerificationRequired", out var ev) && ev.GetBoolean();
        var email = root.TryGetProperty("email", out var em) ? em.GetString() : null;

        return Json(new { success = true, emailVerificationRequired, email });
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

    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            ViewBag.Success = false;
            ViewBag.Message = "Doğrulama bağlantısı geçersiz.";
            return View();
        }

        using var client = CreateApiClient();
        var response = await client.GetAsync($"api/auth/verify-email?token={Uri.EscapeDataString(token)}");
        var json = await response.Content.ReadAsStringAsync();

        ViewBag.Success = response.IsSuccessStatusCode;
        ViewBag.Message = ExtractMessage(json, response.IsSuccessStatusCode
            ? "Email başarıyla doğrulandı."
            : "Doğrulama başarısız.");
        return View();
    }

    [HttpGet]
    public IActionResult ResendVerification() => View();

    [HttpPost]
    public async Task<IActionResult> ResendVerification(string username)
    {
        using var client = CreateApiClient();
        var payload = JsonSerializer.Serialize(new { userName = username });
        var response = await client.PostAsync("api/auth/send-verification-email",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        var json = await response.Content.ReadAsStringAsync();

        ViewBag.Success = response.IsSuccessStatusCode;
        ViewBag.Message = ExtractMessage(json, response.IsSuccessStatusCode
            ? "Doğrulama maili tekrar gönderildi."
            : "Doğrulama maili gönderilemedi.");
        ViewBag.Username = username;
        return View();
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string username)
    {
        using var client = CreateApiClient();
        var payload = JsonSerializer.Serialize(new { userName = username });
        var response = await client.PostAsync("api/auth/forgot-password",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        ViewBag.Submitted = true;
        ViewBag.Success = response.IsSuccessStatusCode;
        ViewBag.Message = "Eğer hesap kayıtlıysa, şifre sıfırlama bağlantısı email adresine gönderildi.";
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string? token)
    {
        ViewBag.Token = token ?? "";
        if (string.IsNullOrWhiteSpace(token))
            ViewBag.Error = "Sıfırlama bağlantısı geçersiz.";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
    {
        ViewBag.Token = token;

        if (string.IsNullOrWhiteSpace(token))
        {
            ViewBag.Error = "Sıfırlama bağlantısı geçersiz.";
            return View();
        }
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            ViewBag.Error = "Şifre en az 6 karakter olmalı.";
            return View();
        }
        if (newPassword != confirmPassword)
        {
            ViewBag.Error = "Şifreler eşleşmiyor.";
            return View();
        }

        using var client = CreateApiClient();
        var payload = JsonSerializer.Serialize(new { token, newPassword });
        var response = await client.PostAsync("api/auth/reset-password",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = ExtractMessage(json, "Şifre sıfırlama başarısız.");
            return View();
        }

        ViewBag.Success = true;
        ViewBag.Message = "Şifreniz başarıyla güncellendi. Giriş yapabilirsiniz.";
        return View();
    }

    private static string ExtractMessage(string json, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var m) && m.GetString() is { } msg)
                return msg;
        }
        catch { }
        return fallback;
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
