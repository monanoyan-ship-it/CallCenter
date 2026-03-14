using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CallCenter.Tests.Factories;

public class IntegrationFactoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IntegrationFactory _sut;
    private readonly AesEncryptionService _aes;

    private const int CustomerId = 1;
    private static readonly Guid CustomerUid = Guid.NewGuid();

    public IntegrationFactoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        // AES service needs config with key
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "TestEncryptionKey_32CharsLong!!"
            })
            .Build();
        _aes = new AesEncryptionService(config);

        var connectionEs = new IntegrationConnectionEntityService(_db);
        var webhookEs = new WebhookSubscriptionEntityService(_db);
        var deliveryEs = new WebhookDeliveryEntityService(_db);
        var apiKeyEs = new IntegrationApiKeyEntityService(_db);
        var uow = new UnitOfWork(_db);

        _sut = new IntegrationFactory(connectionEs, webhookEs, deliveryEs, apiKeyEs, uow, _aes);

        SeedBaseData();
    }

    public void Dispose() => _db.Dispose();

    private void SeedBaseData()
    {
        var customer = new Customer
        {
            Id = CustomerId,
            Uid = CustomerUid,
            Name = "Test Firma",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(customer);
        _db.SaveChanges();
    }

    // ═══════════════════════════════════════════════════════════
    // Connection Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateConnection_ShouldCreateAndReturnId()
    {
        var dto = new CreateIntegrationConnectionRequest
        {
            Name = "Salesforce Test",
            PlatformTypeId = IntegrationPlatforms.Ids.Salesforce,
            AutoLogCalls = true,
            ContactSyncDirectionId = ContactSyncDirections.Ids.Bidirectional
        };

        var (id, error) = await _sut.CreateConnectionAsync(CustomerId, dto);

        id.Should().BeGreaterThan(0);
        error.Should().BeNull();

        var saved = await _db.IntegrationConnections.FindAsync(id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Salesforce Test");
        saved.PlatformTypeId.Should().Be(IntegrationPlatforms.Ids.Salesforce);
        saved.AutoLogCalls.Should().BeTrue();
        saved.IsActive.Should().BeTrue();
        saved.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task CreateConnection_WithCredentials_ShouldEncrypt()
    {
        var dto = new CreateIntegrationConnectionRequest
        {
            Name = "Zendesk Test",
            PlatformTypeId = IntegrationPlatforms.Ids.Zendesk,
            Credentials = new Dictionary<string, string>
            {
                ["Subdomain"] = "testcompany",
                ["Email"] = "admin@test.com",
                ["ApiToken"] = "secret_token_123"
            }
        };

        var (id, _) = await _sut.CreateConnectionAsync(CustomerId, dto);

        var saved = await _db.IntegrationConnections.FindAsync(id);
        saved.Should().NotBeNull();
        saved!.EncryptedCredentials.Should().NotBeNullOrEmpty();
        saved.EncryptedCredentials.Should().NotContain("secret_token_123"); // sifreli olmali
    }

    [Fact]
    public async Task CreateConnection_InvalidPlatform_ShouldFail()
    {
        var dto = new CreateIntegrationConnectionRequest
        {
            Name = "Invalid",
            PlatformTypeId = 999
        };

        var (id, error) = await _sut.CreateConnectionAsync(CustomerId, dto);

        id.Should().Be(0);
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetConnections_ShouldReturnList()
    {
        // Seed connections
        _db.IntegrationConnections.Add(new IntegrationConnection
        {
            CustomerId = CustomerId, Name = "SF", PlatformTypeId = IntegrationPlatforms.Ids.Salesforce,
            IsActive = true, IsConnected = true, CreatedAt = DateTime.UtcNow
        });
        _db.IntegrationConnections.Add(new IntegrationConnection
        {
            CustomerId = CustomerId, Name = "HS", PlatformTypeId = IntegrationPlatforms.Ids.HubSpot,
            IsActive = true, IsConnected = false, CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetConnectionsAsync(CustomerId);

        result.Should().HaveCount(2);
        result.Should().Contain(x => x.Name == "SF");
        result.Should().Contain(x => x.Name == "HS");
    }

    [Fact]
    public async Task GetConnections_ShouldNotReturnOtherCustomers()
    {
        _db.IntegrationConnections.Add(new IntegrationConnection
        {
            CustomerId = CustomerId, Name = "Mine", PlatformTypeId = IntegrationPlatforms.Ids.Salesforce,
            IsActive = true, CreatedAt = DateTime.UtcNow
        });
        _db.IntegrationConnections.Add(new IntegrationConnection
        {
            CustomerId = 999, Name = "Not Mine", PlatformTypeId = IntegrationPlatforms.Ids.HubSpot,
            IsActive = true, CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetConnectionsAsync(CustomerId);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Mine");
    }

    [Fact]
    public async Task UpdateConnection_ShouldModify()
    {
        var conn = new IntegrationConnection
        {
            CustomerId = CustomerId, Name = "Old Name", PlatformTypeId = IntegrationPlatforms.Ids.Salesforce,
            IsActive = true, AutoLogCalls = false, CreatedAt = DateTime.UtcNow
        };
        _db.IntegrationConnections.Add(conn);
        await _db.SaveChangesAsync();

        var (success, _) = await _sut.UpdateConnectionAsync(CustomerId, conn.Uid, new UpdateIntegrationConnectionRequest
        {
            Name = "New Name",
            AutoLogCalls = true
        });

        success.Should().BeTrue();
        var updated = await _db.IntegrationConnections.FindAsync(conn.Id);
        updated!.Name.Should().Be("New Name");
        updated.AutoLogCalls.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteConnection_ShouldRemove()
    {
        var conn = new IntegrationConnection
        {
            CustomerId = CustomerId, Name = "To Delete", PlatformTypeId = IntegrationPlatforms.Ids.Zendesk,
            IsActive = true, CreatedAt = DateTime.UtcNow
        };
        _db.IntegrationConnections.Add(conn);
        await _db.SaveChangesAsync();

        var (success, _) = await _sut.DeleteConnectionAsync(CustomerId, conn.Uid);

        success.Should().BeTrue();
        var deleted = await _db.IntegrationConnections.FindAsync(conn.Id);
        deleted.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════
    // Webhook Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateWebhook_ShouldCreate()
    {
        var dto = new CreateWebhookSubscriptionRequest
        {
            Name = "Test Webhook",
            TargetUrl = "https://example.com/webhook",
            EventFilter = new List<string> { "call.started", "call.ended" }
        };

        var (id, error) = await _sut.CreateWebhookAsync(CustomerId, dto);

        id.Should().BeGreaterThan(0);
        error.Should().BeNull();

        var saved = await _db.WebhookSubscriptions.FindAsync(id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Test Webhook");
        saved.TargetUrl.Should().Be("https://example.com/webhook");
        saved.Secret.Should().NotBeNullOrEmpty(); // auto-generated
        saved.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetWebhooks_ShouldReturnCustomerWebhooks()
    {
        _db.WebhookSubscriptions.Add(new WebhookSubscription
        {
            CustomerId = CustomerId, Name = "WH1", TargetUrl = "https://a.com/wh",
            Secret = "s1", IsActive = true, CreatedAt = DateTime.UtcNow
        });
        _db.WebhookSubscriptions.Add(new WebhookSubscription
        {
            CustomerId = 999, Name = "WH Other", TargetUrl = "https://b.com/wh",
            Secret = "s2", IsActive = true, CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetWebhooksAsync(CustomerId);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("WH1");
    }

    // ═══════════════════════════════════════════════════════════
    // API Key Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateApiKey_ShouldReturnKeyOnce()
    {
        var conn = new IntegrationConnection
        {
            CustomerId = CustomerId, Name = "SF", PlatformTypeId = IntegrationPlatforms.Ids.Salesforce,
            IsActive = true, CreatedAt = DateTime.UtcNow
        };
        _db.IntegrationConnections.Add(conn);
        await _db.SaveChangesAsync();

        var dto = new CreateApiKeyRequest
        {
            Name = "Test Key",
            IntegrationConnectionId = conn.Id,
            Scopes = new List<string> { "calls:read", "contacts:read" }
        };

        var result = await _sut.CreateApiKeyAsync(CustomerId, dto);

        result.Should().NotBeNull();
        result!.ApiKey.Should().StartWith("clk_"); // prefix
        result.ApiKeyPrefix.Should().HaveLength(12); // "clk_" + 8 random chars
        result.Name.Should().Be("Test Key");
        result.Scopes.Should().Contain("calls:read");

        // DB'de hash olarak saklanmis olmali
        var saved = await _db.IntegrationApiKeys.FindAsync(result.Id);
        saved.Should().NotBeNull();
        saved!.ApiKeyHash.Should().NotBeNullOrEmpty();
        saved.ApiKeyHash.Should().NotContain("clk_"); // plain text olmamali
    }

    [Fact]
    public async Task RevokeApiKey_ShouldDeactivate()
    {
        var conn = new IntegrationConnection
        {
            CustomerId = CustomerId, Name = "SF", PlatformTypeId = IntegrationPlatforms.Ids.Salesforce,
            IsActive = true, CreatedAt = DateTime.UtcNow
        };
        _db.IntegrationConnections.Add(conn);
        await _db.SaveChangesAsync();

        var apiKey = new IntegrationApiKey
        {
            CustomerId = CustomerId, IntegrationConnectionId = conn.Id,
            Name = "To Revoke", ApiKeyHash = "hash123", ApiKeyPrefix = "clk_1234",
            Scopes = "[]", IsActive = true, CreatedAt = DateTime.UtcNow
        };
        _db.IntegrationApiKeys.Add(apiKey);
        await _db.SaveChangesAsync();

        var (success, _) = await _sut.RevokeApiKeyAsync(CustomerId, apiKey.Uid);

        success.Should().BeTrue();
        var saved = await _db.IntegrationApiKeys.FindAsync(apiKey.Id);
        saved!.IsActive.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════
    // Static Helper Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void HashApiKey_ShouldBeConsistent()
    {
        var key = "clk_test123456";
        var hash1 = IntegrationFactory.HashApiKey(key);
        var hash2 = IntegrationFactory.HashApiKey(key);

        hash1.Should().Be(hash2);
        hash1.Should().NotBe(key);
    }

    [Fact]
    public void HashApiKey_DifferentKeys_ShouldBeDifferent()
    {
        var hash1 = IntegrationFactory.HashApiKey("clk_key1");
        var hash2 = IntegrationFactory.HashApiKey("clk_key2");

        hash1.Should().NotBe(hash2);
    }
}
