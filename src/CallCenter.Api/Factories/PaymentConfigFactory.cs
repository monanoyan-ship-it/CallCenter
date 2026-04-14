using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services.Payment;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class PaymentConfigFactory : IPaymentConfigFactory
{
    private readonly IPlatformPaymentConfigEntityService _configEs;
    private readonly IUnitOfWork _uow;
    private readonly PaymentGatewayFactory _gatewayFactory;

    public PaymentConfigFactory(IPlatformPaymentConfigEntityService configEs, IUnitOfWork uow, PaymentGatewayFactory gatewayFactory)
    {
        _configEs = configEs;
        _uow = uow;
        _gatewayFactory = gatewayFactory;
    }

    public async Task<List<PaymentConfigListDto>> GetAllAsync()
    {
        var configs = await _configEs.GetAllQueryable()
            .OrderByDescending(c => c.IsActive)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();

        return configs.Select(MapToListDto).ToList();
    }

    public async Task<PaymentConfigDetailDto?> GetByIdAsync(int id)
    {
        var config = await _configEs.GetByIdAsync(id);
        if (config == null) return null;
        return MapToDetailDto(config);
    }

    public async Task<PaymentConfigDetailDto?> GetActiveAsync()
    {
        var config = await _configEs.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.IsActive);
        if (config == null) return null;
        return MapToDetailDto(config);
    }

    public async Task<PaymentBankInfoDto?> GetBankInfoAsync()
    {
        // Aktif config'in banka bilgilerini dondur, yoksa herhangi config'den
        var config = await _configEs.GetAllQueryable()
            .Where(c => c.EncryptedBankInfo != null)
            .OrderByDescending(c => c.IsActive)
            .FirstOrDefaultAsync();

        if (config == null) return null;

        var bankInfo = _gatewayFactory.GetBankInfo(config);
        if (bankInfo == null) return null;

        return new PaymentBankInfoDto
        {
            BankName = bankInfo.BankName,
            IBAN = bankInfo.IBAN,
            AccountHolder = bankInfo.AccountHolder,
            Description = bankInfo.Description
        };
    }

    public async Task<(bool Success, int? Id, string? Error)> CreateAsync(PaymentConfigSaveDto dto)
    {
        // Ayni provider'dan aktif config varsa uyar
        var existing = await _configEs.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.ProviderTypeId == dto.ProviderTypeId);
        if (existing != null)
            return (false, null, $"Bu provider icin zaten bir yapilandirma mevcut (ID: {existing.Id}).");

        var config = new PlatformPaymentConfig
        {
            ProviderTypeId = dto.ProviderTypeId,
            IsSandbox = dto.IsSandbox,
            IsActive = dto.IsActive,
            EncryptedCredentials = EncryptCredentialsFromDto(dto),
            EncryptedBankInfo = EncryptBankInfoFromDto(dto)
        };

        // Eger yeni config aktif ise, diger aktif config'leri pasif yap
        if (config.IsActive)
        {
            var activeConfigs = await _configEs.GetAllQueryable().Where(c => c.IsActive).ToListAsync();
            foreach (var ac in activeConfigs)
                ac.IsActive = false;
        }

        _configEs.Add(config);
        await _uow.SaveChangesAsync();

        return (true, config.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, PaymentConfigSaveDto dto)
    {
        var config = await _configEs.GetByIdAsync(id);
        if (config == null) return (false, "Yapilandirma bulunamadi.");

        config.ProviderTypeId = dto.ProviderTypeId;
        config.IsSandbox = dto.IsSandbox;

        // BUG2.18 fix: Credentials boşsa mevcut olanı koru (edit'te maskeleniyor)
        if (HasAnyCredential(dto))
            config.EncryptedCredentials = EncryptCredentialsFromDto(dto);

        var bankInfo = EncryptBankInfoFromDto(dto);
        if (bankInfo != null)
            config.EncryptedBankInfo = bankInfo;
        config.UpdatedAt = DateTime.UtcNow;

        // Aktif durumu degisiyorsa
        if (dto.IsActive && !config.IsActive)
        {
            var activeConfigs = await _configEs.GetAllQueryable()
                .Where(c => c.IsActive && c.Id != id).ToListAsync();
            foreach (var ac in activeConfigs)
                ac.IsActive = false;
        }
        config.IsActive = dto.IsActive;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var config = await _configEs.GetByIdAsync(id);
        if (config == null) return (false, "Yapilandirma bulunamadi.");

        _configEs.Remove(config);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<PaymentConfigTestResultDto> TestConnectionAsync(int id, CancellationToken ct = default)
    {
        var config = await _configEs.GetByIdAsync(id);
        if (config == null)
            return new PaymentConfigTestResultDto { Success = false, Error = "Yapilandirma bulunamadi.", TestedAt = DateTime.UtcNow };

        try
        {
            var gateway = _gatewayFactory.Create(config);
            var (success, error) = await gateway.TestConnectionAsync(ct);

            config.LastTestedAt = DateTime.UtcNow;
            config.LastTestSuccess = success;
            config.LastTestError = success ? null : error;
            await _uow.SaveChangesAsync();

            return new PaymentConfigTestResultDto { Success = success, Error = error, TestedAt = config.LastTestedAt.Value };
        }
        catch (Exception ex)
        {
            config.LastTestedAt = DateTime.UtcNow;
            config.LastTestSuccess = false;
            config.LastTestError = ex.Message;
            await _uow.SaveChangesAsync();

            return new PaymentConfigTestResultDto { Success = false, Error = ex.Message, TestedAt = config.LastTestedAt.Value };
        }
    }

    public List<PaymentProviderInfoDto> GetAvailableProviders()
    {
        return PaymentProviders.All.Select(p => new PaymentProviderInfoDto
        {
            Id = p.Id,
            Name = p.Description,
            Icon = p.Icon,
            Css = p.CssClass,
            RequiredFields = GetRequiredFields(p.Id)
        }).ToList();
    }

    // ─── Private Helpers ───

    private bool HasAnyCredential(PaymentConfigSaveDto dto)
    {
        return dto.ProviderTypeId switch
        {
            PaymentProviders.Ids.Iyzico => !string.IsNullOrWhiteSpace(dto.IyzicoApiKey) || !string.IsNullOrWhiteSpace(dto.IyzicoSecretKey),
            PaymentProviders.Ids.PayTR => !string.IsNullOrWhiteSpace(dto.PayTrMerchantId) || !string.IsNullOrWhiteSpace(dto.PayTrMerchantKey),
            PaymentProviders.Ids.Param => !string.IsNullOrWhiteSpace(dto.ParamClientCode) || !string.IsNullOrWhiteSpace(dto.ParamClientUsername),
            _ => false
        };
    }

    private string EncryptCredentialsFromDto(PaymentConfigSaveDto dto)
    {
        object creds = dto.ProviderTypeId switch
        {
            PaymentProviders.Ids.Iyzico => new IyzicoCredentials
            {
                ApiKey = dto.IyzicoApiKey ?? "",
                SecretKey = dto.IyzicoSecretKey ?? "",
                BaseUrl = dto.IsSandbox ? "https://sandbox-api.iyzipay.com" : "https://api.iyzipay.com"
            },
            PaymentProviders.Ids.PayTR => new PayTrCredentials
            {
                MerchantId = dto.PayTrMerchantId ?? "",
                MerchantKey = dto.PayTrMerchantKey ?? "",
                MerchantSalt = dto.PayTrMerchantSalt ?? "",
                BaseUrl = "https://www.paytr.com"
            },
            PaymentProviders.Ids.Param => new ParamCredentials
            {
                ClientCode = dto.ParamClientCode ?? "",
                ClientUsername = dto.ParamClientUsername ?? "",
                ClientPassword = dto.ParamClientPassword ?? "",
                Guid = dto.ParamGuid ?? "",
                BaseUrl = dto.IsSandbox
                    ? "https://test-dmz.param.com.tr/turkpos.ws/service_turkpos_test.asmx"
                    : "https://posws.param.com.tr/turkpos.ws/service_turkpos_prod.asmx"
            },
            _ => throw new NotSupportedException($"Provider {dto.ProviderTypeId} desteklenmiyor.")
        };

        return _gatewayFactory.EncryptCredentials(creds);
    }

    private string? EncryptBankInfoFromDto(PaymentConfigSaveDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.BankIBAN)) return null;

        var bankInfo = new BankAccountInfo
        {
            BankName = dto.BankName ?? "",
            IBAN = dto.BankIBAN ?? "",
            AccountHolder = dto.BankAccountHolder ?? "",
            Description = dto.BankDescription
        };

        return _gatewayFactory.EncryptBankInfo(bankInfo);
    }

    private PaymentConfigListDto MapToListDto(PlatformPaymentConfig config)
    {
        var provider = PaymentProviders.GetById(config.ProviderTypeId);
        return new PaymentConfigListDto
        {
            Id = config.Id,
            Uid = config.Uid,
            ProviderTypeId = config.ProviderTypeId,
            ProviderName = provider?.Description ?? "Bilinmeyen",
            ProviderIcon = provider?.Icon ?? "bi-question-circle",
            ProviderCss = provider?.CssClass ?? "bg-secondary",
            IsActive = config.IsActive,
            IsSandbox = config.IsSandbox,
            LastTestedAt = config.LastTestedAt,
            LastTestSuccess = config.LastTestSuccess,
            LastTestError = config.LastTestError,
            CreatedAt = config.CreatedAt
        };
    }

    private PaymentConfigDetailDto MapToDetailDto(PlatformPaymentConfig config)
    {
        var listDto = MapToListDto(config);
        var bankInfo = _gatewayFactory.GetBankInfo(config);

        return new PaymentConfigDetailDto
        {
            Id = listDto.Id,
            Uid = listDto.Uid,
            ProviderTypeId = listDto.ProviderTypeId,
            ProviderName = listDto.ProviderName,
            ProviderIcon = listDto.ProviderIcon,
            ProviderCss = listDto.ProviderCss,
            IsActive = listDto.IsActive,
            IsSandbox = listDto.IsSandbox,
            LastTestedAt = listDto.LastTestedAt,
            LastTestSuccess = listDto.LastTestSuccess,
            LastTestError = listDto.LastTestError,
            CreatedAt = listDto.CreatedAt,
            MaskedCredentials = _gatewayFactory.GetMaskedCredentials(config),
            BankInfo = bankInfo != null ? new PaymentBankInfoDto
            {
                BankName = bankInfo.BankName,
                IBAN = bankInfo.IBAN,
                AccountHolder = bankInfo.AccountHolder,
                Description = bankInfo.Description
            } : null
        };
    }

    private static List<string> GetRequiredFields(int providerTypeId) => providerTypeId switch
    {
        PaymentProviders.Ids.Iyzico => new() { "IyzicoApiKey", "IyzicoSecretKey" },
        PaymentProviders.Ids.PayTR => new() { "PayTrMerchantId", "PayTrMerchantKey", "PayTrMerchantSalt" },
        PaymentProviders.Ids.Param => new() { "ParamClientCode", "ParamClientUsername", "ParamClientPassword", "ParamGuid" },
        _ => new()
    };
}
