using CallCenter.Api.Services;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Tests.Helpers;

namespace CallCenter.Tests.Services;

public class CallForwardingServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CallForwardingService _sut;

    public CallForwardingServiceTests()
    {
        _db = TestDbContextFactory.CreateWithSeedData();
        _sut = new CallForwardingService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ═══════════════════════════════════════
    // GetByUserIdAsync
    // ═══════════════════════════════════════

    [Fact]
    public async Task GetByUserId_ReturnsOnlyUserRules()
    {
        _db.CallForwardingRules.AddRange(
            new CallForwardingRule { UserId = 101, ForwardType = ForwardTypes.Always, Destination = "1111", IsActive = true },
            new CallForwardingRule { UserId = 101, ForwardType = ForwardTypes.Busy, Destination = "2222", IsActive = true },
            new CallForwardingRule { UserId = 102, ForwardType = ForwardTypes.Always, Destination = "3333", IsActive = true }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetByUserIdAsync(101);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.UserId.Should().Be(101));
    }

    // ═══════════════════════════════════════
    // CreateAsync
    // ═══════════════════════════════════════

    [Fact]
    public async Task Create_ValidRule_Succeeds()
    {
        var dto = new CallForwardingRuleCreateDto
        {
            UserId = 101,
            ForwardType = ForwardTypes.NoAnswer,
            Destination = "+905559876543",
            IsActive = true,
            Priority = 1,
            TimeoutSeconds = 25
        };

        var (success, id, error) = await _sut.CreateAsync(dto);

        success.Should().BeTrue();
        id.Should().BeGreaterThan(0);
        error.Should().BeNull();

        var dbRule = await _db.CallForwardingRules.FindAsync(id);
        dbRule.Should().NotBeNull();
        dbRule!.Destination.Should().Be("+905559876543");
        dbRule.TimeoutSeconds.Should().Be(25);
    }

    // ═══════════════════════════════════════
    // DeleteAsync
    // ═══════════════════════════════════════

    [Fact]
    public async Task Delete_ExistingRule_Succeeds()
    {
        var rule = new CallForwardingRule { UserId = 101, ForwardType = ForwardTypes.Busy, Destination = "999" };
        _db.CallForwardingRules.Add(rule);
        await _db.SaveChangesAsync();

        var (success, error) = await _sut.DeleteAsync(rule.Id);

        success.Should().BeTrue();
        var deleted = await _db.CallForwardingRules.FindAsync(rule.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Delete_NonExistentRule_Fails()
    {
        var (success, error) = await _sut.DeleteAsync(99999);

        success.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    // ═══════════════════════════════════════
    // GetForwardDestinationAsync
    // ═══════════════════════════════════════

    [Fact]
    public async Task GetForwardDestination_ActiveAlwaysRule_ReturnsDestination()
    {
        _db.CallForwardingRules.Add(new CallForwardingRule
        {
            UserId = 101,
            ForwardType = ForwardTypes.Always,
            Destination = "+905559999999",
            IsActive = true,
            Priority = 1
        });
        await _db.SaveChangesAsync();

        var dest = await _sut.GetForwardDestinationAsync(101, ForwardTypes.Always);

        dest.Should().Be("+905559999999");
    }

    [Fact]
    public async Task GetForwardDestination_InactiveRule_ReturnsNull()
    {
        _db.CallForwardingRules.Add(new CallForwardingRule
        {
            UserId = 101,
            ForwardType = ForwardTypes.Always,
            Destination = "+905559999999",
            IsActive = false,
            Priority = 1
        });
        await _db.SaveChangesAsync();

        var dest = await _sut.GetForwardDestinationAsync(101, ForwardTypes.Always);

        dest.Should().BeNull();
    }

    [Fact]
    public async Task GetForwardDestination_PriorityOrder_ReturnsHighestPriority()
    {
        _db.CallForwardingRules.AddRange(
            new CallForwardingRule { UserId = 101, ForwardType = ForwardTypes.Busy, Destination = "low-prio", IsActive = true, Priority = 10 },
            new CallForwardingRule { UserId = 101, ForwardType = ForwardTypes.Busy, Destination = "high-prio", IsActive = true, Priority = 1 }
        );
        await _db.SaveChangesAsync();

        var dest = await _sut.GetForwardDestinationAsync(101, ForwardTypes.Busy);

        dest.Should().Be("high-prio");
    }
}
