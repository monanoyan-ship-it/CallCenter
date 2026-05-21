using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CallCenter.Tests.Factories;

public class SlnWaitlistFactoryTests : IDisposable
{
    private readonly AppDbContext _db;

    public SlnWaitlistFactoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
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
    public async Task GetEntriesAsync_AppliesDateAndSearchFilters()
    {
        await SeedSearchFilterEntriesAsync();
        var factory = CreateFactory();

        var dateFiltered = await factory.GetEntriesAsync(1, date: new DateTime(2026, 5, 20), scope: SlnWaitlistStatuses.ScopeActive);
        var clientSearch = await factory.GetEntriesAsync(1, scope: SlnWaitlistStatuses.ScopeActive, search: "ayse");
        var serviceSearch = await factory.GetEntriesAsync(1, scope: SlnWaitlistStatuses.ScopeActive, search: "boya");
        var noteSearch = await factory.GetEntriesAsync(1, scope: SlnWaitlistStatuses.ScopeActive, search: "vip");

        dateFiltered.Select(e => e.Id).Should().BeEquivalentTo(new[] { 201 });
        clientSearch.Select(e => e.Id).Should().BeEquivalentTo(new[] { 201 });
        serviceSearch.Select(e => e.Id).Should().BeEquivalentTo(new[] { 202 });
        noteSearch.Select(e => e.Id).Should().BeEquivalentTo(new[] { 201 });
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
    public async Task CreateAndUpdateEntryAsync_StorePreferredDateAsDateOnlyUtc()
    {
        await SeedOwnershipLookupsAsync(includeEntry: true);
        var factory = CreateFactory();

        var createDto = CreateDto();
        createDto.PreferredDate = new DateTime(2026, 5, 20, 18, 45, 0, DateTimeKind.Local);
        var createResult = await factory.CreateEntryAsync(createDto, 1);

        createResult.Success.Should().BeTrue();
        createResult.Entry!.PreferredDate.Should().Be(new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc));

        var updateDto = CreateUpdateDto();
        updateDto.PreferredDate = new DateTime(2026, 6, 21, 23, 59, 0, DateTimeKind.Local);
        var updateResult = await factory.UpdateEntryAsync(10, updateDto, 1);
        var updatedDate = await _db.SlnWaitlistEntries.AsNoTracking()
            .Where(w => w.Id == 10)
            .Select(w => w.PreferredDate)
            .SingleAsync();

        updateResult.Success.Should().BeTrue();
        updatedDate.Should().Be(new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData(SlnWaitlistStatuses.Ids.Waiting, false)]
    [InlineData(SlnWaitlistStatuses.Ids.Notified, false)]
    [InlineData(SlnWaitlistStatuses.Ids.AppointmentBooked, false)]
    [InlineData(SlnWaitlistStatuses.Ids.Cancelled, true)]
    [InlineData(SlnWaitlistStatuses.Ids.Completed, true)]
    public async Task CreateEntryAsync_PreventsOnlyActiveDuplicates(int existingStatusId, bool expectedSuccess)
    {
        await SeedOwnershipLookupsAsync(includeEntry: true, existingStatusId: existingStatusId);
        var factory = CreateFactory();

        var result = await factory.CreateEntryAsync(CreateDto(), 1);

        result.Success.Should().Be(expectedSuccess);
        if (!expectedSuccess)
            result.Error.Should().Be("Bu tarih icin zaten aktif bekleme kaydi var");
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

    [Fact]
    public async Task ConvertToAppointmentAsync_CreatesAppointmentAndLinksWaitlistEntry()
    {
        await SeedOwnershipLookupsAsync(includeEntry: true);
        var appointmentFactory = Substitute.For<ISlnAppointmentFactory>();
        appointmentFactory
            .CreateAppointmentAsync(
                Arg.Any<SlnAppointmentCreateDto>(),
                7,
                1,
                null)
            .Returns((new SlnAppointmentDto
            {
                Id = 900,
                SlnClientId = 20,
                ClientName = "Client 1",
                PersonnelId = 50,
                PersonnelName = "Staff 1",
                ServiceIds = [30],
                ServiceNames = ["Service 1"],
                StartTime = new DateTime(2026, 5, 21, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 5, 21, 10, 30, 0, DateTimeKind.Utc),
                StatusId = 1
            }, null));
        var factory = CreateFactory(appointmentFactory);

        var result = await factory.ConvertToAppointmentAsync(10, new SlnWaitlistConvertToAppointmentDto
        {
            PersonnelId = 50,
            BranchId = 40,
            StartTime = new DateTime(2026, 5, 21, 10, 0, 0, DateTimeKind.Utc),
            Notes = "Donustur"
        }, 7, 1);

        result.Success.Should().BeTrue();
        result.Result!.Appointment.Id.Should().Be(900);
        result.Result.WaitlistEntry.SlnAppointmentId.Should().Be(900);
        var entry = await _db.SlnWaitlistEntries.AsNoTracking().SingleAsync(w => w.Id == 10);
        entry.StatusId.Should().Be(SlnWaitlistStatuses.Ids.AppointmentBooked);
        entry.SlnAppointmentId.Should().Be(900);
    }

    private SlnWaitlistFactory CreateFactory(ISlnAppointmentFactory? appointmentFactory = null)
        => new(
            new SlnWaitlistEntryEntityService(_db),
            new SlnClientEntityService(_db),
            appointmentFactory ?? Substitute.For<ISlnAppointmentFactory>(),
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

    private async Task SeedOwnershipLookupsAsync(bool includeEntry = false, int existingStatusId = SlnWaitlistStatuses.Ids.Waiting)
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
                StatusId = existingStatusId
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

    private async Task SeedSearchFilterEntriesAsync()
    {
        _db.Customers.Add(new Customer
        {
            Id = 1,
            Uid = Guid.NewGuid(),
            Name = "Test Salon",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _db.SlnClients.AddRange(
            new SlnClient { Id = 20, CustomerId = 1, FullName = "Ayse Kara", Phone = "5551112233", IsActive = true },
            new SlnClient { Id = 21, CustomerId = 1, FullName = "Mehmet Ak", Phone = "5559998877", IsActive = true });
        _db.SlnServices.AddRange(
            new SlnService { Id = 30, CustomerId = 1, Name = "Kesim", DurationMinutes = 30, Price = 100m, IsActive = true },
            new SlnService { Id = 31, CustomerId = 1, Name = "Boya", DurationMinutes = 60, Price = 200m, IsActive = true });
        _db.SlnWaitlistEntries.AddRange(
            new SlnWaitlistEntry
            {
                Id = 201,
                CustomerId = 1,
                SlnClientId = 20,
                ServiceId = 30,
                PreferredDate = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc),
                Notes = "VIP musteri",
                StatusId = SlnWaitlistStatuses.Ids.Waiting
            },
            new SlnWaitlistEntry
            {
                Id = 202,
                CustomerId = 1,
                SlnClientId = 21,
                ServiceId = 31,
                PreferredDate = new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc),
                Notes = "Normal",
                StatusId = SlnWaitlistStatuses.Ids.Waiting
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
