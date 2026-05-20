using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CallCenter.Tests.Factories;

public class SlnAppointmentFactoryTests : IDisposable
{
    private readonly AppDbContext _db;

    public SlnAppointmentFactoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task UpdateStatusAsync_CompletedMultiServiceAppointment_ConsumesRecipeBranchStock()
    {
        SeedRecipeAppointment();
        _db.ChangeTracker.Clear();

        var factory = CreateFactory();

        var (success, error, _) = await factory.UpdateStatusAsync(30, 3, 1);

        success.Should().BeTrue(error);
        var branchStock = await _db.SlnProductBranchStocks.SingleAsync(s => s.ProductId == 20 && s.BranchId == 3);
        branchStock.StockQuantity.Should().Be(8m);
        (await _db.SlnStockMovements.CountAsync(m => m.Notes != null && m.Notes.StartsWith("Randevu:30")))
            .Should().Be(1);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ReturnsEmpty_WhenPersonnelBelongsToAnotherCustomer()
    {
        SeedSlotScopeData();
        _db.ChangeTracker.Clear();

        var factory = CreateFactory();

        var slots = await factory.GetAvailableSlotsAsync(1, 21, new DateTime(2026, 5, 20), 30, branchId: 3, serviceIds: [7]);

        slots.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ReturnsEmpty_WhenBranchScopeDoesNotMatchPersonnel()
    {
        SeedSlotScopeData();
        _db.ChangeTracker.Clear();

        var factory = CreateFactory();

        var slots = await factory.GetAvailableSlotsAsync(1, 14, new DateTime(2026, 5, 20), 30, branchId: 3, serviceIds: [7]);

        slots.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ReturnsEmpty_WhenServiceIsOutsideCustomer()
    {
        SeedSlotScopeData();
        _db.ChangeTracker.Clear();

        var factory = CreateFactory();

        var slots = await factory.GetAvailableSlotsAsync(1, 11, new DateTime(2026, 5, 20), 30, branchId: 3, serviceIds: [80]);

        slots.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ReturnsEmpty_WhenPersonnelLacksRequiredSkill()
    {
        SeedSlotScopeData();
        _db.SlnPersonnelSkills.Add(new SlnPersonnelSkill
        {
            Id = 90,
            PersonnelId = 11,
            ServiceId = 7
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var factory = CreateFactory();

        var slots = await factory.GetAvailableSlotsAsync(1, 14, new DateTime(2026, 5, 20), 30, serviceIds: [7]);

        slots.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_UsesPersonnelBranchHours_WhenBranchIsNotRequested()
    {
        SeedSlotScopeData();
        _db.ChangeTracker.Clear();

        var factory = CreateFactory();

        var slots = await factory.GetAvailableSlotsAsync(1, 14, new DateTime(2026, 5, 20), 30, serviceIds: [7]);

        slots.Select(slot => GetAnonymousValue<string>(slot, "timeText"))
            .Should().Equal("10:00", "10:30");
    }

    private SlnAppointmentFactory CreateFactory()
        => new(
            new SlnAppointmentEntityService(_db),
            new SlnServiceEntityService(_db),
            new SlnClientEntityService(_db),
            new SlnNoShowPolicyEntityService(_db),
            new SlnPersonnelSkillEntityService(_db),
            new SlnServiceComboEntityService(_db),
            new SlnServiceResourceRequirementEntityService(_db),
            new SlnRecipeEntityService(_db),
            new SlnProductEntityService(_db),
            new SlnStockMovementEntityService(_db),
            new CustomerPersonnelEntityService(_db),
            new SlnBranchEntityService(_db),
            new SlnStockBalanceService(
                new SlnProductBranchStockEntityService(_db),
                new SlnBranchEntityService(_db)),
            new UnitOfWork(_db),
            NullLogger<SlnAppointmentFactory>.Instance);

    private static T GetAnonymousValue<T>(object source, string propertyName)
        => (T)(source.GetType().GetProperty(propertyName)?.GetValue(source)
            ?? throw new InvalidOperationException($"Property '{propertyName}' was not found."));

    private void SeedRecipeAppointment()
    {
        _db.Customers.Add(new Customer
        {
            Id = 1,
            Uid = Guid.NewGuid(),
            Name = "Test Salon",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _db.SlnBranches.Add(new SlnBranch
        {
            Id = 3,
            CustomerId = 1,
            Name = "Merkez",
            Slug = "test-salon",
            IsHeadquarter = true,
            IsActive = true
        });
        _db.SlnClients.Add(new SlnClient
        {
            Id = 10,
            CustomerId = 1,
            FullName = "Test Musteri",
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
            IsActive = true,
            CustomerRoleId = SalonRoles.Ids.Hairdresser
        });
        _db.SlnServiceCategories.Add(new SlnServiceCategory
        {
            Id = 6,
            CustomerId = 1,
            Name = "Bakim",
            IsActive = true
        });
        _db.SlnServices.AddRange(
            new SlnService
            {
                Id = 7,
                CustomerId = 1,
                CategoryId = 6,
                Name = "Receteli Hizmet",
                DurationMinutes = 30,
                Price = 100m,
                IsActive = true
            },
            new SlnService
            {
                Id = 8,
                CustomerId = 1,
                CategoryId = 6,
                Name = "Diger Hizmet",
                DurationMinutes = 30,
                Price = 100m,
                IsActive = true
            });
        _db.SlnProductCategories.Add(new SlnProductCategory
        {
            Id = 15,
            CustomerId = 1,
            Name = "Sarf"
        });
        _db.SlnProducts.Add(new SlnProduct
        {
            Id = 20,
            CustomerId = 1,
            CategoryId = 15,
            Name = "Boya",
            PurchasePrice = 10m,
            SalePrice = 20m,
            StockQuantity = 10m,
            Unit = "Adet",
            IsActive = true
        });
        _db.SlnProductBranchStocks.Add(new SlnProductBranchStock
        {
            Id = 21,
            CustomerId = 1,
            ProductId = 20,
            BranchId = 3,
            StockQuantity = 10m
        });
        _db.SlnRecipes.Add(new SlnRecipe
        {
            Id = 22,
            CustomerId = 1,
            Name = "Boya Recetesi",
            ServiceId = 7,
            IsActive = true,
            Items =
            [
                new SlnRecipeItem
                {
                    Id = 23,
                    ProductId = 20,
                    Quantity = 2m,
                    Unit = "Adet",
                    Cost = 20m
                }
            ]
        });
        _db.SlnAppointments.Add(new SlnAppointment
        {
            Id = 30,
            CustomerId = 1,
            BranchId = 3,
            SlnClientId = 10,
            PersonnelId = 11,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            StatusId = 2,
            Services =
            [
                new SlnAppointmentService { Id = 31, SlnServiceId = 7, SortOrder = 1 },
                new SlnAppointmentService { Id = 32, SlnServiceId = 8, SortOrder = 2 }
            ]
        });

        _db.SaveChanges();
    }

    private void SeedSlotScopeData()
    {
        _db.Customers.AddRange(
            new Customer
            {
                Id = 1,
                Uid = Guid.NewGuid(),
                Name = "Test Salon",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Customer
            {
                Id = 2,
                Uid = Guid.NewGuid(),
                Name = "Other Salon",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        _db.SlnBranches.AddRange(
            new SlnBranch
            {
                Id = 3,
                CustomerId = 1,
                Name = "Merkez",
                Slug = "test-salon",
                WorkingHoursJson = "{\"wed\":\"closed\"}",
                IsHeadquarter = true,
                IsActive = true
            },
            new SlnBranch
            {
                Id = 4,
                CustomerId = 1,
                Name = "Sube",
                Slug = "test-salon-sube",
                WorkingHoursJson = "{\"wed\":\"10:00-11:00\"}",
                IsActive = true
            },
            new SlnBranch
            {
                Id = 13,
                CustomerId = 2,
                Name = "Other Branch",
                Slug = "other-salon",
                IsHeadquarter = true,
                IsActive = true
            });
        _db.Users.AddRange(
            CreateUser(12, "staff@test.local", "Salon Personel"),
            CreateUser(15, "staff2@test.local", "Sube Personel"),
            CreateUser(22, "other@test.local", "Other Personel"));
        _db.CustomerPersonnel.AddRange(
            new CustomerPersonnel
            {
                Id = 11,
                CustomerId = 1,
                UserId = 12,
                IsActive = true,
                CustomerRoleId = SalonRoles.Ids.Hairdresser,
                BranchId = 3
            },
            new CustomerPersonnel
            {
                Id = 14,
                CustomerId = 1,
                UserId = 15,
                IsActive = true,
                CustomerRoleId = SalonRoles.Ids.Hairdresser,
                BranchId = 4
            },
            new CustomerPersonnel
            {
                Id = 21,
                CustomerId = 2,
                UserId = 22,
                IsActive = true,
                CustomerRoleId = SalonRoles.Ids.Hairdresser,
                BranchId = 13
            });
        _db.SlnServiceCategories.AddRange(
            new SlnServiceCategory
            {
                Id = 6,
                CustomerId = 1,
                Name = "Bakim",
                IsActive = true
            },
            new SlnServiceCategory
            {
                Id = 60,
                CustomerId = 2,
                Name = "Other Bakim",
                IsActive = true
            });
        _db.SlnServices.AddRange(
            new SlnService
            {
                Id = 7,
                CustomerId = 1,
                CategoryId = 6,
                Name = "Kesim",
                DurationMinutes = 30,
                Price = 100m,
                IsActive = true
            },
            new SlnService
            {
                Id = 80,
                CustomerId = 2,
                CategoryId = 60,
                Name = "Other Kesim",
                DurationMinutes = 30,
                Price = 100m,
                IsActive = true
            });

        _db.SaveChanges();
    }

    private static User CreateUser(int id, string email, string fullName) => new()
    {
        Id = id,
        Uid = Guid.NewGuid(),
        UserName = email,
        FullName = fullName,
        Email = email,
        PasswordHash = "hash",
        RoleId = UserRoles.Ids.Agent,
        StatusId = AgentStatuses.Ids.Available,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
}
