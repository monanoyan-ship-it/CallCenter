using System.Net.Http.Json;
using CallCenter.Shared.DTOs;
using Microsoft.JSInterop;

namespace CallCenter.Web.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly JwtAuthStateProvider _authStateProvider;

    private const string TokenKey = "auth_token";
    private const string FullNameKey = "auth_fullname";
    private const string RoleKey = "auth_role";

    public AuthService(HttpClient http, IJSRuntime js, JwtAuthStateProvider authStateProvider)
    {
        _http = http;
        _js = js;
        _authStateProvider = authStateProvider;
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
            await _js.InvokeVoidAsync("localStorage.setItem", FullNameKey, loginResponse.FullName);
            await _js.InvokeVoidAsync("localStorage.setItem", RoleKey, loginResponse.Role);

            // Auth state'i guncelle
            _authStateProvider.NotifyUserAuthentication(loginResponse.Token);

            return new LoginResult(true, null);
        }
        catch (Exception ex)
        {
            return new LoginResult(false, $"Baglanti hatasi: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", FullNameKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", RoleKey);

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

    // Yardimci siniflar
    public record LoginResult(bool Success, string? ErrorMessage);
    private record ErrorResponse(string? Message);
}
