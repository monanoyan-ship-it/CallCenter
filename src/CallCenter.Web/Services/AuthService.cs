using System.Net.Http.Json;
using CallCenter.Shared.DTOs;
using Microsoft.JSInterop;

namespace CallCenter.Web.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly JwtAuthStateProvider _authStateProvider;
    private readonly PermissionService _permService;

    private const string TokenKey = "auth_token";
    private const string RefreshTokenKey = "auth_refresh_token";
    private const string FullNameKey = "auth_fullname";
    private const string RoleKey = "auth_role";

    public AuthService(HttpClient http, IJSRuntime js, JwtAuthStateProvider authStateProvider, PermissionService permService)
    {
        _http = http;
        _js = js;
        _authStateProvider = authStateProvider;
        _permService = permService;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                return new LoginResult(false, error?.Message ?? "Giris basarisiz.");
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (loginResponse == null)
                return new LoginResult(false, "Sunucu yaniti okunamadi.");

            // Token ve bilgileri localStorage'a kaydet
            await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, loginResponse.Token);
            await _js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, loginResponse.RefreshToken);
            await _js.InvokeVoidAsync("localStorage.setItem", FullNameKey, loginResponse.FullName);
            await _js.InvokeVoidAsync("localStorage.setItem", RoleKey, loginResponse.Role);

            // Auth state'i guncelle
            _authStateProvider.NotifyUserAuthentication(loginResponse.Token);

            // MustChangePassword flag'ini kaydet
            if (loginResponse.MustChangePassword)
                await _js.InvokeVoidAsync("localStorage.setItem", "must_change_pw", "true");

            return new LoginResult(true, null, loginResponse.MustChangePassword, loginResponse.Role);
        }
        catch (Exception ex)
        {
            return new LoginResult(false, $"Baglanti hatasi: {ex.Message}");
        }
    }

    /// <summary>
    /// Refresh token ile yeni access token alir. Basarili olursa yeni token'lari kaydeder.
    /// </summary>
    public async Task<bool> TryRefreshTokenAsync()
    {
        try
        {
            var refreshToken = await _js.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            var response = await _http.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequest { RefreshToken = refreshToken });

            if (!response.IsSuccessStatusCode)
            {
                // Refresh basarisiz — logout
                await ForceLogoutAsync();
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
            if (result == null)
                return false;

            // Yeni token'lari kaydet
            await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, result.Token);
            await _js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, result.RefreshToken);

            // Auth state'i guncelle
            _authStateProvider.NotifyUserAuthentication(result.Token);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        // Server'daki refresh token'i iptal et
        try
        {
            var refreshToken = await _js.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _http.PostAsJsonAsync("api/auth/revoke", new RefreshTokenRequest { RefreshToken = refreshToken });
            }
        }
        catch
        {
            // Revoke basarisiz olsa bile logout devam etsin
        }

        await ForceLogoutAsync();
    }

    /// <summary>Sadece local storage temizler, server'a istek gitmez.</summary>
    public async Task ForceLogoutAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", FullNameKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", RoleKey);

        _permService.Reset();
        _authStateProvider.NotifyUserLogout();
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
    }

    public async Task<string?> GetFullNameAsync()
    {
        return await _js.InvokeAsync<string?>("localStorage.getItem", FullNameKey);
    }

    public async Task<string?> GetRoleAsync()
    {
        return await _js.InvokeAsync<string?>("localStorage.getItem", RoleKey);
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/change-password", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                return new ChangePasswordResult(false, error?.Message ?? "Sifre degistirme basarisiz.");
            }

            // Flag'i temizle
            await _js.InvokeVoidAsync("localStorage.removeItem", "must_change_pw");
            return new ChangePasswordResult(true, null);
        }
        catch (Exception ex)
        {
            return new ChangePasswordResult(false, $"Baglanti hatasi: {ex.Message}");
        }
    }

    // Yardimci siniflar
    public record LoginResult(bool Success, string? ErrorMessage, bool MustChangePassword = false, string? Role = null);
    public record ChangePasswordResult(bool Success, string? ErrorMessage);
    private record ErrorResponse(string? Message);
}
