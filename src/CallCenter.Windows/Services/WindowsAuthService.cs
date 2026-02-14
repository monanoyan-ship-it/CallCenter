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
    private const string RefreshTokenKey = "auth_refresh_token";
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
            await _storage.SetAsync(RefreshTokenKey, loginResponse.RefreshToken);
            await _storage.SetAsync(FullNameKey, loginResponse.FullName);
            await _storage.SetAsync(RoleKey, loginResponse.Role);

            // Auth state'i guncelle
            _authStateProvider.NotifyUserAuthentication(loginResponse.Token);

            // MustChangePassword flag'ini kaydet
            if (loginResponse.MustChangePassword)
                await _storage.SetAsync("must_change_pw", "true");

            return new LoginResult(true, null, loginResponse.MustChangePassword);
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
            var refreshToken = await _storage.GetAsync(RefreshTokenKey);
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            var response = await _http.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequest { RefreshToken = refreshToken });

            if (!response.IsSuccessStatusCode)
            {
                await ForceLogoutAsync();
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
            if (result == null)
                return false;

            // Yeni token'lari kaydet
            await _storage.SetAsync(TokenKey, result.Token);
            await _storage.SetAsync(RefreshTokenKey, result.RefreshToken);

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
            var refreshToken = await _storage.GetAsync(RefreshTokenKey);
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
    private async Task ForceLogoutAsync()
    {
        await _storage.RemoveAsync(TokenKey);
        await _storage.RemoveAsync(RefreshTokenKey);
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
            await _storage.RemoveAsync("must_change_pw");
            return new ChangePasswordResult(true, null);
        }
        catch (Exception ex)
        {
            return new ChangePasswordResult(false, $"Baglanti hatasi: {ex.Message}");
        }
    }

    // Yardimci siniflar
    public record LoginResult(bool Success, string? ErrorMessage, bool MustChangePassword = false);
    public record ChangePasswordResult(bool Success, string? ErrorMessage);
    private record ErrorResponse(string? Message);
}
