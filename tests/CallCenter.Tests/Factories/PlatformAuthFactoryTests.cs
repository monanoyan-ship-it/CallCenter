using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Api.Services.Email;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Helpers;
using CallCenter.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace CallCenter.Tests.Factories;

public class PlatformAuthFactoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PlatformAuthFactory _sut;

    public PlatformAuthFactoryTests()
    {
        _db = TestDbContextFactory.Create();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
                ["Jwt:Issuer"] = "tests",
                ["Jwt:Audience"] = "tests",
                ["Jwt:ExpireMinutes"] = "60"
            })
            .Build();

        _sut = new PlatformAuthFactory(
            new PlatformUserEntityService(_db),
            new TokenService(config),
            new UnitOfWork(_db),
            Substitute.For<IPlatformEmailService>(),
            config,
            new MemoryCache(new MemoryCacheOptions()));
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task LoginAsync_LocksAccountKeyAfterRepeatedWrongPasswords()
    {
        var phone = PhoneHelper.Normalize("05001234567")!;
        _db.PlatformUsers.Add(new PlatformUser
        {
            FullName = "Test User",
            Phone = phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct1!"),
            IsActive = true
        });
        await _db.SaveChangesAsync();

        for (var i = 0; i < 5; i++)
        {
            var (result, error) = await _sut.LoginAsync(new PlatformLoginDto
            {
                Phone = "05001234567",
                Password = "Wrong1!"
            });

            result.Should().BeNull();
            error.Should().NotBeNullOrWhiteSpace();
        }

        var locked = await _sut.LoginAsync(new PlatformLoginDto
        {
            Phone = "05001234567",
            Password = "Correct1!"
        });

        locked.Result.Should().BeNull();
        locked.Error.Should().Contain("Cok fazla basarisiz giris");
    }
}
