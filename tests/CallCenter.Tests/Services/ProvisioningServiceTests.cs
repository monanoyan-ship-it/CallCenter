using CallCenter.Api.Services;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CallCenter.Tests.Services;

public class ProvisioningServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ProvisioningService _sut;

    public ProvisioningServiceTests()
    {
        _db = TestDbContextFactory.CreateWithSeedData();
        var sipAccountService = NSubstitute.Substitute.For<ISipAccountService>();
        _sut = new ProvisioningService(_db, sipAccountService, NullLogger<ProvisioningService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ═══════════════════════════════════════
    // Token yaratma + one-time kullanim
    // ═══════════════════════════════════════

    [Fact]
    public async Task CreateProvisioning_GeneratesUniqueToken()
    {
        var req = new CreateProvisioningRequest { UserId = 101 };

        var result1 = await _sut.CreateProvisioningAsync(req, "https://api.test.local");
        var result2 = await _sut.CreateProvisioningAsync(req, "https://api.test.local");

        result1.Token.Should().NotBeNullOrEmpty();
        result2.Token.Should().NotBeNullOrEmpty();
        result1.Token.Should().NotBe(result2.Token);
        result1.Url.Should().Contain("token=");
    }

    [Fact]
    public async Task GetByToken_ValidToken_ReturnsConfigAndInvalidatesToken()
    {
        var req = new CreateProvisioningRequest { UserId = 101 };
        var created = await _sut.CreateProvisioningAsync(req, "https://api.test.local");

        // Ilk kullanim
        var config = await _sut.GetProvisioningByTokenAsync(created.Token);
        config.Should().NotBeNull();

        // Ikinci kullanim (one-time token tukenmis olmali)
        var config2 = await _sut.GetProvisioningByTokenAsync(created.Token);
        config2.Should().BeNull();
    }

    [Fact]
    public async Task GetByToken_InvalidToken_ReturnsNull()
    {
        var config = await _sut.GetProvisioningByTokenAsync("gecersiz-token-12345");

        config.Should().BeNull();
    }
}
