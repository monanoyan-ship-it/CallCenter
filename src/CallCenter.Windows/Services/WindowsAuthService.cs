using System.Net.Http;
using System.Net.Http.Json;
using CallCenter.Shared.DTOs;

namespace CallCenter.Windows.Services;

/// <summary>
/// Windows uygulamasi icin kimlik dogrulama servisi.
/// Web'deki AuthService'in SecureStorage ile adapte edilmis hali.
/// </summary>
public class WindowsAuthService
{
    private readonly HttpClient _http;
    private readonly SecureStorage _storage;
    private readonly WindowsAuthStateProvider _authStateProvider;

    private const string TokenKey = "auth_token";
    private const string FullNameKey = "auth_fullname";
    private const string RoleKey = "auth_role";

    public WindowsAuthService(HttpClient http, SecureStorage storage, WindowsAuthStateProvider authStateProvider)
    {
        _http = http;
        _storage = storage;
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

            // Token ve bilgileri SecureStorage'a kaydet
            await _storage.SetAsync(TokenKey, loginResponse.Token);
            await _storage.SetAsync(FullNameKey, loginResponse.FullName);
            await _storage.SetAsync(RoleKey, loginResponse.Role);

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
        await _storage.RemoveAsync(TokenKey);
        await _storage.RemoveAsync(FullNameKey);
        await _storage.RemoveAsync(RoleKey);

        _authStateProvider.NotifyUserLogout();
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _storage.GetAsync(TokenKey);
    }

    public async Task<string?> GetFullNameAsync()
    {
        return await _storage.GetAsync(FullNameKey);
    }

    public async Task<string?> GetRoleAsync()
    {
        return await _storage.GetAsync(RoleKey);
    }

    // Yardimci siniflar
    public record LoginResult(bool Success, string? ErrorMessage);
    private record ErrorResponse(string? Message);
}
