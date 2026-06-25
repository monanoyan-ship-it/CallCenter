using CallCenter.Api.EntityServices;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CallCenter.Tests.Services;

/// <summary>
/// Hat rotasyonu (round-robin): her tahsiste sonraki hatta gec, sona gelince basa don,
/// dolu hatlari atla, hepsi doluysa null. Pointer Customer.LastAssignedSipLineId'de tutulur.
/// </summary>
public class SipLineRotationTests
{
    private static AppDbContext BuildDbWithLines(int lineCount)
    {
        var db = TestDbContextFactory.Create();
        db.Customers.Add(new Customer { Id = 1, Uid = Guid.NewGuid(), Name = "C", IsActive = true });
        db.SipAccounts.Add(new SipAccount
        {
            Id = 10, Uid = Guid.NewGuid(), CustomerId = 1, Name = "GW",
            Server = "s", Port = 5060, IsDefault = true, IsActive = true
        });
        for (var i = 1; i <= lineCount; i++)
            db.SipLines.Add(new SipLine
            {
                Id = 100 + i, SipAccountId = 10, ChannelNumber = i,
                Username = $"100{i}", Password = "x", IsActive = true
            });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task Rotates_Through_All_Lines_Then_Wraps()
    {
        using var db = BuildDbWithLines(3);
        var es = new SipLineEntityService(db);

        var picked = new List<int>();
        for (var call = 0; call < 4; call++)
        {
            var line = await es.AcquireNextRotatingAsync(1);
            line.Should().NotBeNull();
            picked.Add(line!.ChannelNumber);
            await db.SaveChangesAsync(); // pointer kalici

            // Hatti hemen serbest birak: sadece rotasyon pointer'inin ilerleyisini olc.
            line.AssignedPersonnelId = null;
            await db.SaveChangesAsync();
        }

        picked.Should().Equal(1, 2, 3, 1); // 1 -> 2 -> 3 -> basa don
    }

    [Fact]
    public async Task Skips_Assigned_Lines()
    {
        using var db = BuildDbWithLines(3);
        var es = new SipLineEntityService(db);

        var first = await es.AcquireNextRotatingAsync(1);
        await db.SaveChangesAsync();
        first!.ChannelNumber.Should().Be(1);
        first.AssignedPersonnelId = 1; // 1 artik dolu
        await db.SaveChangesAsync();

        // Hat 2'yi de baskasi tutuyor
        var line2 = await db.SipLines.FirstAsync(l => l.ChannelNumber == 2);
        line2.AssignedPersonnelId = 999;
        await db.SaveChangesAsync();

        var next = await es.AcquireNextRotatingAsync(1);
        await db.SaveChangesAsync();
        next!.ChannelNumber.Should().Be(3); // 2 dolu atlanir -> 3
    }

    [Fact]
    public async Task Returns_Null_When_All_Assigned()
    {
        using var db = BuildDbWithLines(2);
        foreach (var l in db.SipLines) l.AssignedPersonnelId = 5;
        await db.SaveChangesAsync();
        var es = new SipLineEntityService(db);

        var line = await es.AcquireNextRotatingAsync(1);
        line.Should().BeNull();
    }

    [Fact]
    public async Task PointerPersists_AcrossInstances_SimulatingLogoutLogin()
    {
        using var db = BuildDbWithLines(3);

        var first = await new SipLineEntityService(db).AcquireNextRotatingAsync(1);
        await db.SaveChangesAsync();
        first!.AssignedPersonnelId = null; // logout -> hat serbest
        await db.SaveChangesAsync();
        first.ChannelNumber.Should().Be(1);

        // Yeni login (yeni ES ornegi) ayni DB -> pointer korunur -> sonraki hat
        var second = await new SipLineEntityService(db).AcquireNextRotatingAsync(1);
        await db.SaveChangesAsync();
        second!.ChannelNumber.Should().Be(2);
    }
}
