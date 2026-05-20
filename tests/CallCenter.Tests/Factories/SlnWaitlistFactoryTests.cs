using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Tests.Factories;

public class SlnWaitlistFactoryTests : IDisposable
{
    private readonly AppDbContext _db;

    public SlnWaitlistFactoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task UpdateStatusAsync_RejectsUndefinedStatus()
    {
        await SeedWaitlistEntryAsync(SlnWaitlistStatuses.Ids.Waiting);
        var factory = CreateFactory();

        var (success, error) = await factory.UpdateStatusAsync(10, 999, 1);

        success.Should().BeFalse();
        error.Should().Be("Gecersiz bekleme listesi durumu");
        (await GetStatusAsync()).Should().Be(SlnWaitlistStatuses.Ids.Waiting);
    }

    [Fact]
    public async Task UpdateStatusAsync_RejectsInvalidTransition()
    {
        await SeedWaitlistEntryAsync(SlnWaitlistStatuses.Ids.Waiting);
        var factory = CreateFactory();

        var (success, error) = await factory.UpdateStatusAsync(10, SlnWaitlistStatuses.Ids.Completed, 1);

        success.Should().BeFalse();
        error.Should().Be("Bu bekleme listesi durum gecisi yapilamaz");
        (await GetStatusAsync()).Should().Be(SlnWaitlistStatuses.Ids.Waiting);
    }

    [Fact]
    public async Task UpdateStatusAsync_AllowsLifecycleAndPreservesNotifiedAt()
    {
        await SeedWaitlistEntryAsync(SlnWaitlistStatuses.Ids.Waiting);
        var factory = CreateFactory();

        (await factory.UpdateStatusAsync(10, SlnWaitlistStatuses.Ids.Notified, 1)).Success.Should().BeTrue();
        var notifiedAt = await _db.SlnWaitlistEntries.AsNoTracking()
            .Where(w => w.Id == 10)
            .Select(w => w.NotifiedAt)
            .SingleAsync();
        notifiedAt.Should().NotBeNull();

        (await factory.UpdateStatusAsync(10, SlnWaitlistStatuses.Ids.Notified, 1)).Success.Should().BeTrue();
        var secondNotifiedAt = await _db.SlnWaitlistEntries.AsNoTracking()
            .Where(w => w.Id == 10)
            .Select(w => w.NotifiedAt)
            .SingleAsync();
        secondNotifiedAt.Should().Be(notifiedAt);

        (await factory.UpdateStatusAsync(10, SlnWaitlistStatuses.Ids.AppointmentBooked, 1)).Success.Should().BeTrue();
        (await factory.UpdateStatusAsync(10, SlnWaitlistStatuses.Ids.Completed, 1)).Success.Should().BeTrue();
        (await GetStatusAsync()).Should().Be(SlnWaitlistStatuses.Ids.Completed);
    }

    [Fact]
    public async Task GetEntriesAsync_AppliesActiveAndArchiveScopes()
    {
        await SeedWaitlistEntriesAsync();
        var factory = CreateFactory();

        (await _db.SlnWaitlistEntries.CountAsync()).Should().Be(5);
        (await _db.SlnWaitlistEntries
            .CountAsync(w => w.StatusId == SlnWaitlistStatuses.Ids.Waiting
                          || w.StatusId == SlnWaitlistStatuses.Ids.Notified
                          || w.StatusId == SlnWaitlistStatuses.Ids.AppointmentBooked))
            .Should().Be(3);

        var active = await factory.GetEntriesAsync(1, scope: SlnWaitlistStatuses.ScopeActive);
        var archive = await factory.GetEntriesAsync(1, scope: SlnWaitlistStatuses.ScopeArchive);
        var all = await factory.GetEntriesAsync(1, scope: SlnWaitlistStatuses.ScopeAll);

        active.Select(e => e.StatusId).Should().BeEquivalentTo(new[]
        {
            SlnWaitlistStatuses.Ids.Waiting,
            SlnWaitlistStatuses.Ids.Notified,
            SlnWaitlistStatuses.Ids.AppointmentBooked
        });
        archive.Select(e => e.StatusId).Should().BeEquivalentTo(new[]
        {
            SlnWaitlistStatuses.Ids.Cancelled,
            SlnWaitlistStatuses.Ids.Completed
        });
        all.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetEntriesAsync_ReturnsStatusMetadata()
    {
        await SeedWaitlistEntriesAsync();
        var factory = CreateFactory();

        var entries = await factory.GetEntriesAsync(1, scope: SlnWaitlistStatuses.ScopeActive);
        var booked = entries.Single(e => e.StatusId == SlnWaitlistStatuses.Ids.AppointmentBooked);

        booked.StatusName.Should().Be(SlnWaitlistStatuses.AppointmentBooked.Description);
        booked.StatusSystemName.Should().Be(SlnWaitlistStatuses.AppointmentBooked.SystemName);
        booked.StatusTranslationKey.Should().Be(SlnWaitlistStatuses.AppointmentBooked.NameResourceKey);
        booked.StatusCssClass.Should().Be(SlnWaitlistStatuses.AppointmentBooked.CssClass);
        booked.IsActive.Should().BeTrue();
        booked.IsArchived.Should().BeFalse();
        booked.IsTerminal.Should().BeFalse();
    }

    private SlnWaitlistFactory CreateFactory()
        => new(
            new SlnWaitlistEntryEntityService(_db),
            new SlnBranchEntityService(_db),
            new CustomerPersonnelEntityService(_db),
            new UnitOfWork(_db));

    private async Task SeedWaitlistEntryAsync(int statusId)
    {
        _db.Customers.Add(new Customer
        {
            Id = 1,
            Uid = Guid.NewGuid(),
            Name = "Test Salon",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _db.SlnWaitlistEntries.Add(new SlnWaitlistEntry
        {
            Id = 10,
            CustomerId = 1,
            SlnClientId = 20,
            ServiceId = 30,
            PreferredDate = DateTime.UtcNow.Date,
            StatusId = statusId
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    private async Task SeedWaitlistEntriesAsync()
    {
        _db.Customers.Add(new Customer
        {
            Id = 1,
            Uid = Guid.NewGuid(),
            Name = "Test Salon",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _db.SlnClients.Add(new SlnClient
        {
            Id = 20,
            CustomerId = 1,
            FullName = "Test Musteri",
            IsActive = true
        });
        _db.SlnServices.Add(new SlnService
        {
            Id = 30,
            CustomerId = 1,
            Name = "Test Hizmet",
            DurationMinutes = 30,
            Price = 100m,
            IsActive = true
        });

        foreach (var statusId in SlnWaitlistStatuses.All.Select(s => s.Id))
        {
            _db.SlnWaitlistEntries.Add(new SlnWaitlistEntry
            {
                Id = 100 + statusId,
                CustomerId = 1,
                SlnClientId = 20,
                ServiceId = 30,
                PreferredDate = DateTime.UtcNow.Date,
                StatusId = statusId,
                CreatedAt = DateTime.UtcNow.AddMinutes(statusId)
            });
        }

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    private Task<int> GetStatusAsync()
        => _db.SlnWaitlistEntries.AsNoTracking()
            .Where(w => w.Id == 10)
            .Select(w => w.StatusId)
            .SingleAsync();
}
