using System.Collections.Concurrent;
using System.Security.Cryptography;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class ProvisioningService : IProvisioningService
{
    private readonly AppDbContext _db;
    private readonly ISipAccountService _sipService;
    private readonly ILogger<ProvisioningService> _logger;

    // In-memory token store (production'da Redis/DB kullanilmali)
    private static readonly ConcurrentDictionary<string, ProvisioningTokenInfo> _tokens = new();

    public ProvisioningService(AppDbContext db, ISipAccountService sipService, ILogger<ProvisioningService> logger)
    {
        _db = db;
        _sipService = sipService;
        _logger = logger;
    }

    public async Task<ProvisioningUrlResponse> CreateProvisioningAsync(CreateProvisioningRequest req, string baseUrl)
    {
        // Kullanici var mi kontrol et
        var user = await _db.Users.FindAsync(req.UserId);
        if (user == null)
            throw new InvalidOperationException("Kullanici bulunamadi");

        // Token olustur (cryptographic random)
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-").Replace("/", "_").Replace("=", ""); // URL-safe

        var expiresAt = req.ExpiresInHours > 0
            ? DateTime.UtcNow.AddHours(req.ExpiresInHours)
            : DateTime.UtcNow.AddYears(1); // Sinirsiz (1 yil)

        _tokens[token] = new ProvisioningTokenInfo
        {
            UserId = req.UserId,
            ExpiresAt = expiresAt,
            UiSettings = req.Ui ?? new ProvisioningUiSettings(),
            ExpiresInHours = req.ExpiresInHours
        };

        var url = $"{baseUrl.TrimEnd('/')}/api/provisioning/config?token={token}";

        _logger.LogInformation("Provisioning token olusturuldu: UserId={UserId}, ExpiresAt={ExpiresAt}", req.UserId, expiresAt);

        return new ProvisioningUrlResponse
        {
            Url = url,
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    public async Task<ProvisioningConfigDto?> GetProvisioningByTokenAsync(string token)
    {
        // Token gecerli mi?
        if (!_tokens.TryGetValue(token, out var tokenInfo))
        {
            _logger.LogWarning("Provisioning token bulunamadi: {Token}", token[..Math.Min(8, token.Length)]);
            return null;
        }

        if (DateTime.UtcNow > tokenInfo.ExpiresAt)
        {
            _tokens.TryRemove(token, out _);
            _logger.LogWarning("Provisioning token suresi dolmus: UserId={UserId}", tokenInfo.UserId);
            return null;
        }

        var config = await BuildConfigAsync(tokenInfo.UserId, tokenInfo.UiSettings, tokenInfo.ExpiresInHours);

        // One-time: token'i sil (tekrar kullanilamaz)
        _tokens.TryRemove(token, out _);

        return config;
    }

    public async Task<ProvisioningConfigDto?> GetMyProvisioningAsync(int userId)
    {
        return await BuildConfigAsync(userId, new ProvisioningUiSettings(), 24);
    }

    private async Task<ProvisioningConfigDto?> BuildConfigAsync(int userId, ProvisioningUiSettings uiSettings, int expiresInHours)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;

        // SIP baglanti bilgisi
        SipConnectionInfoDto? sipConnection = null;
        try
        {
            // Kullanicinin musteri ID'sini bul
            var customerId = await _db.CustomerPersonnel
                .Where(cp => cp.UserId == userId)
                .Select(cp => cp.CustomerId)
                .FirstOrDefaultAsync();

            if (customerId > 0)
            {
                sipConnection = await _sipService.GetMyConnectionAsync(customerId, user.FullName);
            }
        }
        catch
        {
            _logger.LogWarning("Provisioning: SIP bilgisi alinamadi, UserId={UserId}", userId);
        }

        // Config version = User.UpdatedAt veya SipAccount.UpdatedAt hash'i
        var version = $"v{DateTime.UtcNow:yyyyMMddHHmm}-{userId}";

        return new ProvisioningConfigDto
        {
            Version = version,
            UserId = userId,
            DisplayName = user.FullName,
            SipConnection = sipConnection,
            Ui = uiSettings,
            CreatedAt = DateTime.UtcNow,
            ExpiresInHours = expiresInHours
        };
    }

    /// <summary>Token bilgisi (in-memory)</summary>
    private class ProvisioningTokenInfo
    {
        public int UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public ProvisioningUiSettings UiSettings { get; set; } = new();
        public int ExpiresInHours { get; set; }
    }
}
