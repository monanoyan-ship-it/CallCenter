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

    private Task<int> GetStatusAsync()
        => _db.SlnWaitlistEntries.AsNoTracking()
            .Where(w => w.Id == 10)
            .Select(w => w.StatusId)
            .SingleAsync();
}
