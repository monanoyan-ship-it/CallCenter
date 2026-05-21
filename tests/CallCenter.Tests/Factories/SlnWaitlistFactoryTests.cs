using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
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

    [Fact]
    public async Task CreateEntryAsync_RejectsLookupIdsOutsideCustomer()
    {
        await SeedOwnershipLookupsAsync();
        var factory = CreateFactory();

        (await factory.CreateEntryAsync(CreateDto(slnClientId: 21), 1)).Should().Match<(bool Success, string? Error, SlnWaitlistEntryDto? Entry)>(
            result => !result.Success && result.Error == "Musteri bulunamadi" && result.Entry == null);
        (await factory.CreateEntryAsync(CreateDto(serviceId: 31), 1)).Should().Match<(bool Success, string? Error, SlnWaitlistEntryDto? Entry)>(
            result => !result.Success && result.Error == "Hizmet bulunamadi" && result.Entry == null);
        (await factory.CreateEntryAsync(CreateDto(preferredPersonnelId: 51), 1)).Should().Match<(bool Success, string? Error, SlnWaitlistEntryDto? Entry)>(
            result => !result.Success && result.Error == "Personel bulunamadi" && result.Entry == null);
        (await factory.CreateEntryAsync(CreateDto(branchId: 41), 1)).Should().Match<(bool Success, string? Error, SlnWaitlistEntryDto? Entry)>(
            result => !result.Success && result.Error == "Sube bulunamadi" && result.Entry == null);
        (await factory.CreateEntryAsync(CreateDto(branchId: 40), 1, branchScopeId: 42)).Should().Match<(bool Success, string? Error, SlnWaitlistEntryDto? Entry)>(
            result => !result.Success && result.Error == "Bu sube icin yetkiniz yok" && result.Entry == null);
    }

    [Fact]
    public async Task UpdateEntryAsync_RejectsLookupIdsOutsideCustomer()
    {
        await SeedOwnershipLookupsAsync(includeEntry: true);
        var factory = CreateFactory();

        var serviceResult = await factory.UpdateEntryAsync(10, CreateUpdateDto(serviceId: 31), 1);
        var personnelResult = await factory.UpdateEntryAsync(10, CreateUpdateDto(preferredPersonnelId: 51), 1);
        var branchResult = await factory.UpdateEntryAsync(10, CreateUpdateDto(branchId: 41), 1);

        serviceResult.Success.Should().BeFalse();
        serviceResult.Error.Should().Be("Hizmet bulunamadi");
        personnelResult.Success.Should().BeFalse();
        personnelResult.Error.Should().Be("Personel bulunamadi");
        branchResult.Success.Should().BeFalse();
        branchResult.Error.Should().Be("Sube bulunamadi");
    }

    private SlnWaitlistFactory CreateFactory()
        => new(
            new SlnWaitlistEntryEntityService(_db),
            new SlnClientEntityService(_db),
            new SlnServiceEntityService(_db),
            new SlnBranchEntityService(_db),
            new CustomerPersonnelEntityService(_db),
            new UnitOfWork(_db));

    private static SlnWaitlistEntryCreateDto CreateDto(
        int slnClientId = 20,
        int serviceId = 30,
        int? preferredPersonnelId = 50,
        int? branchId = 40)
        => new()
        {
            SlnClientId = slnClientId,
            ServiceId = serviceId,
            PreferredPersonnelId = preferredPersonnelId,
            BranchId = branchId,
            PreferredDate = DateTime.UtcNow.Date
        };

    private static SlnWaitlistEntryUpdateDto CreateUpdateDto(
        int slnClientId = 20,
        int serviceId = 30,
        int? preferredPersonnelId = 50,
        int? branchId = 40)
        => new()
        {
            SlnClientId = slnClientId,
            ServiceId = serviceId,
            PreferredPersonnelId = preferredPersonnelId,
            BranchId = branchId,
            PreferredDate = DateTime.UtcNow.Date
        };

    private async Task SeedOwnershipLookupsAsync(bool includeEntry = false)
    {
        _db.Customers.AddRange(
            new Customer { Id = 1, Uid = Guid.NewGuid(), Name = "Salon 1", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Customer { Id = 2, Uid = Guid.NewGuid(), Name = "Salon 2", IsActive = true, CreatedAt = DateTime.UtcNow });
        _db.SlnClients.AddRange(
            new SlnClient { Id = 20, CustomerId = 1, FullName = "Client 1", IsActive = true },
            new SlnClient { Id = 21, CustomerId = 2, FullName = "Client 2", IsActive = true });
        _db.SlnServices.AddRange(
            new SlnService { Id = 30, CustomerId = 1, Name = "Service 1", DurationMinutes = 30, Price = 100m, IsActive = true },
            new SlnService { Id = 31, CustomerId = 2, Name = "Service 2", DurationMinutes = 30, Price = 100m, IsActive = true });
        _db.SlnBranches.AddRange(
            new SlnBranch { Id = 40, CustomerId = 1, Name = "Branch 1", IsActive = true },
            new SlnBranch { Id = 41, CustomerId = 2, Name = "Branch 2", IsActive = true },
            new SlnBranch { Id = 42, CustomerId = 1, Name = "Branch 3", IsActive = true });
        _db.CustomerPersonnel.AddRange(
            new CustomerPersonnel { Id = 50, CustomerId = 1, UserId = 50, BranchId = 40, IsActive = true },
            new CustomerPersonnel { Id = 51, CustomerId = 2, UserId = 51, BranchId = 41, IsActive = true });

        if (includeEntry)
        {
            _db.SlnWaitlistEntries.Add(new SlnWaitlistEntry
            {
                Id = 10,
                CustomerId = 1,
                SlnClientId = 20,
                ServiceId = 30,
                BranchId = 40,
                PreferredPersonnelId = 50,
                PreferredDate = DateTime.UtcNow.Date,
                StatusId = SlnWaitlistStatuses.Ids.Waiting
            });
        }

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

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
