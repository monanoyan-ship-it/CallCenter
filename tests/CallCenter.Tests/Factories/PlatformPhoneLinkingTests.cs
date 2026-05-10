using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CallCenter.Tests.Factories;

public class PlatformPhoneLinkingTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PlatformFactory _platformFactory;
    private readonly SlnPublicFactory _publicFactory;

    public PlatformPhoneLinkingTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _platformFactory = CreatePlatformFactory();
        _publicFactory = CreatePublicFactory();
        SeedBaseData();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetMyAppointments_FindsPublicBookingClient_WhenPhoneFormatsDiffer()
    {
        _db.SlnClients.Add(new SlnClient
        {
            Id = 10,
            CustomerId = 1,
            FullName = "Mobil Musteri",
            Phone = "05060716728",
            IsActive = true
        });
        _db.SlnAppointments.Add(new SlnAppointment
        {
            Id = 20,
            CustomerId = 1,
            SlnClientId = 10,
            ServiceId = 7,
            PersonnelId = 11,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            StatusId = 2,
            IsPrepaid = true,
            PrepaidAmount = 50m
        });
        await _db.SaveChangesAsync();

        var result = await _platformFactory.GetMyAppointmentsAsync(30, past: false);

        result.Should().ContainSingle(a => a.Id == 20);
        result[0].ServiceNames.Should().Contain("Sac Kesim");
    }

    [Fact]
    public async Task GetMySalons_DerivesSalonFromPaidPublicBooking_WhenPhoneFormatsDiffer()
    {
        _db.SlnClients.Add(new SlnClient
        {
            Id = 10,
            CustomerId = 1,
            FullName = "Mobil Musteri",
            Phone = "05060716728",
            IsActive = true,
            CreatedAt = new DateTime(2026, 5, 10, 9, 0, 0, DateTimeKind.Utc)
        });
        _db.SlnAppointments.Add(new SlnAppointment
        {
            Id = 20,
            CustomerId = 1,
            SlnClientId = 10,
            ServiceId = 7,
            PersonnelId = 11,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            StatusId = 2
        });
        await _db.SaveChangesAsync();

        var result = await _platformFactory.GetMySalonsAsync(30);

        result.Should().ContainSingle(s => s.CustomerId == 1);
    }

    [Fact]
    public async Task JoinSalon_ReusesLegacyClientAndNormalizesPhone()
    {
        _db.SlnClients.Add(new SlnClient
        {
            Id = 10,
            CustomerId = 1,
            FullName = "Mobil Musteri",
            Phone = "05060716728",
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var (success, error) = await _platformFactory.JoinSalonAsync(30, 1);

        success.Should().BeTrue(error);
        var link = await _db.PlatformUserSalons.SingleAsync();
        link.SlnClientId.Should().Be(10);
        (await _db.SlnClients.FindAsync(10))!.Phone.Should().Be("+905060716728");
    }

    [Fact]
    public async Task PublicBooking_CreatesSalonLink_WhenPlatformUserAlreadyExists()
    {
        var start = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var dto = new SlnOnlineBookingDto
        {
            FullName = "Mobil Musteri",
            Phone = "05060716728",
            Email = "mobil@test.local",
            ServiceId = 7,
            PersonnelId = 11,
            StartTime = start
        };

        var (success, error, result) = await _publicFactory.BookAppointmentAsync("test-salon", dto);

        success.Should().BeTrue(error);
        result.Should().NotBeNull();
        var client = await _db.SlnClients.SingleAsync();
        client.Phone.Should().Be("+905060716728");
        var link = await _db.PlatformUserSalons.SingleAsync();
        link.PlatformUserId.Should().Be(30);
        link.CustomerId.Should().Be(1);
        link.SlnClientId.Should().Be(client.Id);
    }

    private PlatformFactory CreatePlatformFactory()
        => new(
            new PlatformUserSalonEntityService(_db),
            new PlatformUserEntityService(_db),
            new CustomerEntityService(_db),
            new SlnClientEntityService(_db),
            new SlnAppointmentEntityService(_db),
            new SlnAppointmentServiceEntityService(_db),
            new SlnServiceEntityService(_db),
            new SlnSalonProfileEntityService(_db),
            new SlnBranchEntityService(_db),
            new SlnClientMembershipEntityService(_db),
            new SlnClientLoyaltyEntityService(_db),
            new SlnGiftCardEntityService(_db),
            new SlnNoShowPolicyEntityService(_db),
            CreatePaymentService(),
            new UnitOfWork(_db));

    private SlnPublicFactory CreatePublicFactory()
        => new(
            new SlnSalonProfileEntityService(_db),
            new SlnBranchEntityService(_db),
            new SlnServiceCategoryEntityService(_db),
            new SlnServiceEntityService(_db),
            new SlnReviewEntityService(_db),
            new CustomerPersonnelEntityService(_db),
            new SlnMembershipPlanEntityService(_db),
            new SlnClientMembershipEntityService(_db),
            new SlnClientEntityService(_db),
            new SlnAppointmentEntityService(_db),
            new SlnAppointmentServiceEntityService(_db),
            new SlnPersonnelSkillEntityService(_db),
            new SlnNoShowPolicyEntityService(_db),
            new SlnWaitlistEntryEntityService(_db),
            new PlatformUserEntityService(_db),
            new PlatformUserSalonEntityService(_db),
            CreatePaymentService(),
            new UnitOfWork(_db));

    private PaymentService CreatePaymentService()
        => new(_db, null!, null!, NullLogger<PaymentService>.Instance);

    private void SeedBaseData()
    {
        _db.Customers.Add(new Customer
        {
            Id = 1,
            Uid = Guid.NewGuid(),
            Name = "Test Salon",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _db.SlnSalonProfiles.Add(new SlnSalonProfile
        {
            Id = 2,
            CustomerId = 1,
            Slug = "test-salon",
            IsPublished = true,
            City = "Istanbul",
            District = "Kadikoy"
        });
        _db.SlnBranches.Add(new SlnBranch
        {
            Id = 3,
            CustomerId = 1,
            Name = "Merkez",
            Slug = "test-salon",
            IsHeadquarter = true,
            IsActive = true,
            City = "Istanbul",
            District = "Kadikoy"
        });
        _db.SlnServiceCategories.Add(new SlnServiceCategory
        {
            Id = 6,
            CustomerId = 1,
            Name = "Sac",
            IsActive = true
        });
        _db.SlnServices.Add(new SlnService
        {
            Id = 7,
            CustomerId = 1,
            CategoryId = 6,
            Name = "Sac Kesim",
            DurationMinutes = 30,
            Price = 100m,
            IsActive = true
        });
        _db.Users.Add(new User
        {
            Id = 12,
            Uid = Guid.NewGuid(),
            UserName = "staff",
            FullName = "Salon Personel",
            Email = "staff@test.local",
            PasswordHash = "hash",
            RoleId = UserRoles.Ids.Agent,
            StatusId = AgentStatuses.Ids.Available,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _db.CustomerPersonnel.Add(new CustomerPersonnel
        {
            Id = 11,
            CustomerId = 1,
            UserId = 12,
            Title = "Uzman",
            IsActive = true,
            PublicVisible = true,
            CustomerRoleId = SalonRoles.Ids.Hairdresser
        });
        _db.PlatformUsers.Add(new PlatformUser
        {
            Id = 30,
            FullName = "Mobil Musteri",
            Phone = "+905060716728",
            Email = "mobil@test.local",
            PasswordHash = "hash",
            IsActive = true
        });

        _db.SaveChanges();
    }
}
