using System.Collections.Concurrent;
using System.Security.Cryptography;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CallCenter.Api.Factories;

public class ProvisioningFactory : IProvisioningFactory
{
    private readonly IUserEntityService _userEs;
    private readonly ICustomerPersonnelEntityService _personnelEs;
    private readonly ISipAccountFactory _sipFactory;
    private readonly ILogger<ProvisioningFactory> _logger;

    private static readonly ConcurrentDictionary<string, ProvisioningTokenInfo> _tokens = new();

    public ProvisioningFactory(
        IUserEntityService userEs,
        ICustomerPersonnelEntityService personnelEs,
        ISipAccountFactory sipFactory,
        ILogger<ProvisioningFactory> logger)
    {
        _userEs = userEs;
        _personnelEs = personnelEs;
        _sipFactory = sipFactory;
        _logger = logger;
    }

    public async Task<ProvisioningUrlResponse> CreateProvisioningAsync(CreateProvisioningRequest req, string baseUrl)
    {
        var user = await _userEs.GetByIdAsync(req.UserId);
        if (user == null)
            throw new InvalidOperationException("Kullanici bulunamadi");

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        var expiresAt = req.ExpiresInHours > 0
            ? DateTime.UtcNow.AddHours(req.ExpiresInHours)
            : DateTime.UtcNow.AddYears(1);

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

        _tokens.TryRemove(token, out _);

        return config;
    }

    public async Task<ProvisioningConfigDto?> GetMyProvisioningAsync(int userId)
    {
        return await BuildConfigAsync(userId, new ProvisioningUiSettings(), 24);
    }

    private async Task<ProvisioningConfigDto?> BuildConfigAsync(int userId, ProvisioningUiSettings uiSettings, int expiresInHours)
    {
        var user = await _userEs.GetByIdAsync(userId);
        if (user == null) return null;

        SipConnectionInfoDto? sipConnection = null;
        try
        {
            var personnel = await _personnelEs.GetAllQueryable()
                .Where(cp => cp.UserId == userId)
                .Select(cp => new { cp.CustomerId, cp.Id })
                .FirstOrDefaultAsync();

            if (personnel != null && personnel.CustomerId > 0)
            {
                sipConnection = await _sipFactory.GetMyConnectionAsync(personnel.CustomerId, personnel.Id, user.FullName);
            }
        }
        catch
        {
            _logger.LogWarning("Provisioning: SIP bilgisi alinamadi, UserId={UserId}", userId);
        }

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

    private class ProvisioningTokenInfo
    {
        public int UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public ProvisioningUiSettings UiSettings { get; set; } = new();
        public int ExpiresInHours { get; set; }
    }
}
