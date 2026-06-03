using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Api.Services.Payment;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CallCenter.Tests.Factories;

public sealed class PaymentConfigFactoryTests : IDisposable
{
    private readonly CallCenter.Data.AppDbContext _db;
    private readonly PaymentGatewayFactory _gatewayFactory;
    private readonly PaymentConfigFactory _sut;

    public PaymentConfigFactoryTests()
    {
        _db = TestDbContextFactory.Create();
        _gatewayFactory = CreateGatewayFactory();
        _sut = new PaymentConfigFactory(
            new PlatformPaymentConfigEntityService(_db),
            new UnitOfWork(_db),
            _gatewayFactory);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateAsync_RejectsEmptyCredentials()
    {
        var result = await _sut.CreateAsync(new PaymentConfigSaveDto
        {
            ProviderTypeId = PaymentProviders.Ids.PayTR,
            IsSandbox = true,
            IsActive = false
        });

        result.Success.Should().BeFalse();
        result.Id.Should().BeNull();
        result.Error.Should().Contain("PayTR").And.Contain(nameof(PaymentConfigSaveDto.PayTrMerchantId));
        (await _db.PlatformPaymentConfigs.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_RejectsPartialCredentials()
    {
        var config = await SeedIyzicoConfigAsync();
        var encryptedCredentialsBefore = config.EncryptedCredentials;

        var result = await _sut.UpdateAsync(config.Id, new PaymentConfigSaveDto
        {
            ProviderTypeId = PaymentProviders.Ids.Iyzico,
            IsSandbox = true,
            IsActive = true,
            IyzicoApiKey = "new-api-key"
        });

        result.Success.Should().BeFalse();
        result.Error.Should().Contain(nameof(PaymentConfigSaveDto.IyzicoSecretKey));

        var persisted = await _db.PlatformPaymentConfigs.AsNoTracking().SingleAsync(c => c.Id == config.Id);
        persisted.EncryptedCredentials.Should().Be(encryptedCredentialsBefore);
    }

    [Fact]
    public async Task UpdateAsync_PreservesCredentials_WhenCredentialFieldsAreEmpty()
    {
        var config = await SeedIyzicoConfigAsync();
        var encryptedCredentialsBefore = config.EncryptedCredentials;

        var result = await _sut.UpdateAsync(config.Id, new PaymentConfigSaveDto
        {
            ProviderTypeId = PaymentProviders.Ids.Iyzico,
            IsSandbox = true,
            IsActive = false
        });

        result.Success.Should().BeTrue();

        var persisted = await _db.PlatformPaymentConfigs.AsNoTracking().SingleAsync(c => c.Id == config.Id);
        persisted.EncryptedCredentials.Should().Be(encryptedCredentialsBefore);
        persisted.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_PersistsConfig_WhenAllCredentialsArePresent()
    {
        var result = await _sut.CreateAsync(new PaymentConfigSaveDto
        {
            ProviderTypeId = PaymentProviders.Ids.PayTR,
            IsSandbox = true,
            IsActive = false,
            PayTrMerchantId = "merchant-id",
            PayTrMerchantKey = "merchant-key",
            PayTrMerchantSalt = "merchant-salt"
        });

        result.Success.Should().BeTrue();
        result.Id.Should().NotBeNull();

        var persisted = await _db.PlatformPaymentConfigs.AsNoTracking().SingleAsync();
        persisted.EncryptedCredentials.Should().NotBeNullOrWhiteSpace();
    }

    private async Task<PlatformPaymentConfig> SeedIyzicoConfigAsync()
    {
        var config = new PlatformPaymentConfig
        {
            Id = 1,
            ProviderTypeId = PaymentProviders.Ids.Iyzico,
            IsSandbox = true,
            IsActive = true,
            EncryptedCredentials = _gatewayFactory.EncryptCredentials(new IyzicoCredentials
            {
                ApiKey = "sandbox-api-key",
                SecretKey = "sandbox-secret-key",
                BaseUrl = "https://sandbox-api.iyzipay.com"
            }),
            CreatedAt = DateTime.UtcNow
        };

        _db.PlatformPaymentConfigs.Add(config);
        await _db.SaveChangesAsync();
        return config;
    }

    private static PaymentGatewayFactory CreateGatewayFactory()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "CallCenter_Tests_Encryption_Key_2026"
            })
            .Build();

        return new PaymentGatewayFactory(new AesEncryptionService(config));
    }
}
