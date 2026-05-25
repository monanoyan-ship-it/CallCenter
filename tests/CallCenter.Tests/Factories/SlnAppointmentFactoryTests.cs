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
    public async Task AppointmentIdActions_RespectBranchScope()
    {
        SeedRecipeAppointment();
        _db.SlnBranches.Add(new SlnBranch
        {
            Id = 4,
            CustomerId = 1,
            Name = "Sube",
            Slug = "test-salon-sube",
            IsActive = true
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var factory = CreateFactory();

        var inScope = await factory.GetAppointmentAsync(30, 1, branchId: 3);
        var outOfScope = await factory.GetAppointmentAsync(30, 1, branchId: 4);
        var updateDto = new SlnAppointmentCreateDto
        {
            SlnClientId = 10,
            PersonnelId = 11,
            ServiceIds = [7],
            StartTime = DateTime.UtcNow.AddDays(1)
        };
        var (updated, updateError) = await factory.UpdateAppointmentAsync(30, updateDto, 1, branchId: 4);
        var (statusUpdated, statusError, _) = await factory.UpdateStatusAsync(30, 3, 1, branchId: 4);
        var (deleted, deleteError) = await factory.DeleteAppointmentAsync(30, 1, branchId: 4);

        inScope.Should().NotBeNull();
        outOfScope.Should().BeNull();
        updated.Should().BeFalse();
        updateError.Should().Be("Randevu bulunamadi");
        statusUpdated.Should().BeFalse();
        statusError.Should().Be("Randevu bulunamadi");
        deleted.Should().BeFalse();
        deleteError.Should().Be("Randevu bulunamadi");

        var appointment = await _db.SlnAppointments.SingleAsync(a => a.Id == 30);
        appointment.StatusId.Should().Be(2);
    }

    [Fact]
    public async Task GetAppointmentsAsync_WithClientFilter_StillRespectsBranchScope()
    {
        SeedRecipeAppointment();
        _db.SlnBranches.Add(new SlnBranch
        {
            Id = 4,
            CustomerId = 1,
            Name = "Diger Sube",
            Slug = "test-salon-diger-sube",
            IsActive = true
        });
        _db.SlnAppointments.Add(new SlnAppointment
        {
            Id = 40,
            CustomerId = 1,
            BranchId = 4,
            SlnClientId = 10,
            PersonnelId = 11,
            StartTime = DateTime.UtcNow.AddHours(3),
            EndTime = DateTime.UtcNow.AddHours(4),
            StatusId = 2,
            Services =
            [
                new SlnAppointmentService { Id = 41, SlnServiceId = 7, SortOrder = 1 }
            ]
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var factory = CreateFactory();

        var appointments = await factory.GetAppointmentsAsync(1, null, null, branchId: 3, slnClientId: 10);

        appointments.Select(a => a.Id).Should().Equal(30);
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
    public async Task GetAvailableStaffAsync_ReturnsEmpty_WhenServiceIsOutsideCustomer()
    {
        SeedSlotScopeData();
        _db.ChangeTracker.Clear();

        var factory = CreateFactory();

        var staff = await factory.GetAvailableStaffAsync(1, [80], branchId: 3);

        staff.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableStaffAsync_FiltersPersonnelByBranchScope()
    {
        SeedSlotScopeData();
        _db.ChangeTracker.Clear();

        var factory = CreateFactory();

        var staff = await factory.GetAvailableStaffAsync(1, [7], branchId: 3);

        staff.Select(item => GetAnonymousValue<int>(item, "Id"))
            .Should().Equal(11);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_DoesNotCountOtherBranchAppointmentsForBranchResource()
    {
        SeedSlotScopeData();
        _db.SlnClients.Add(new SlnClient
        {
            Id = 10,
            CustomerId = 1,
            FullName = "Test Musteri",
            IsActive = true
        });
        _db.SlnResources.Add(new SlnResource
        {
            Id = 30,
            CustomerId = 1,
            BranchId = 4,
            Name = "Branch Room",
            Quantity = 1,
            IsActive = true
        });
        _db.SlnServiceResourceRequirements.Add(new SlnServiceResourceRequirement
        {
            Id = 31,
            ServiceId = 7,
            ResourceId = 30,
            QuantityRequired = 1
        });
        _db.SlnAppointments.Add(new SlnAppointment
        {
            Id = 40,
            CustomerId = 1,
            BranchId = 3,
            SlnClientId = 10,
            PersonnelId = 11,
            StartTime = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 5, 20, 10, 30, 0, DateTimeKind.Utc),
            StatusId = 2,
            Services =
            [
                new SlnAppointmentService { Id = 41, SlnServiceId = 7, SortOrder = 1 }
            ]
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var factory = CreateFactory();

        var slots = await factory.GetAvailableSlotsAsync(1, 14, new DateTime(2026, 5, 20), 30, branchId: 4, serviceIds: [7]);
        var ten = slots.Single(slot => GetAnonymousValue<string>(slot, "timeText") == "10:00");

        GetAnonymousValue<bool>(ten, "available").Should().BeTrue();
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

    [Fact]
    public async Task GetAppointmentsAsync_ReturnsPostPayPaidAmount()
    {
        SeedRecipeAppointment();
        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            CustomerId = 1,
            PaymentTypeId = PaymentTypes.Ids.SalonAdisyon,
            PaymentMethodId = 1,
            StatusId = PaymentStatuses.Ids.Basarili,
            Amount = 125m,
            Notes = "PayAppointment:30|MarketplaceSplit"
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var factory = CreateFactory();

        var appointments = await factory.GetAppointmentsAsync(1, null, null);

        appointments.Single(a => a.Id == 30).PaidAmount.Should().Be(125m);
    }

    [Fact]
    public async Task GetAppointmentsAsync_ExpiresStaleAwaitingPaymentAppointments()
    {
        SeedRecipeAppointment();
        var stale = await _db.SlnAppointments.SingleAsync(a => a.Id == 30);
        stale.StatusId = 6;
        stale.IsPrepaid = false;
        stale.CreatedAt = DateTime.UtcNow - PaymentService.PendingPaymentHoldTimeout - TimeSpan.FromMinutes(1);
        _db.SlnAppointments.Add(new SlnAppointment
        {
            Id = 40,
            CustomerId = 1,
            BranchId = 3,
            SlnClientId = 10,
            PersonnelId = 11,
            StartTime = DateTime.UtcNow.AddHours(3),
            EndTime = DateTime.UtcNow.AddHours(4),
            StatusId = 6,
            IsPrepaid = false,
            CreatedAt = DateTime.UtcNow,
            Services =
            [
                new SlnAppointmentService { Id = 41, SlnServiceId = 7, SortOrder = 1 }
            ]
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var factory = CreateFactory();

        var awaitingPayment = await factory.GetAppointmentsAsync(1, null, null, statusId: 6, branchId: 3);

        awaitingPayment.Select(a => a.Id).Should().Equal(40);
        var statuses = await _db.SlnAppointments.AsNoTracking()
            .Where(a => a.Id == 30 || a.Id == 40)
            .ToDictionaryAsync(a => a.Id, a => a.StatusId);
        statuses[30].Should().Be(4);
        statuses[40].Should().Be(6);
    }

    [Fact]
    public async Task GetAppointmentAsync_ExpiresStaleAwaitingPaymentAppointment()
    {
        SeedRecipeAppointment();
        var stale = await _db.SlnAppointments.SingleAsync(a => a.Id == 30);
        stale.StatusId = 6;
        stale.IsPrepaid = false;
        stale.CreatedAt = DateTime.UtcNow - PaymentService.PendingPaymentHoldTimeout - TimeSpan.FromMinutes(1);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var factory = CreateFactory();

        var appointment = await factory.GetAppointmentAsync(30, 1, branchId: 3);

        appointment.Should().NotBeNull();
        appointment!.StatusId.Should().Be(4);
        var status = await _db.SlnAppointments.AsNoTracking()
            .Where(a => a.Id == 30)
            .Select(a => a.StatusId)
            .SingleAsync();
        status.Should().Be(4);
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
            new PaymentService(_db, null!, null!, NullLogger<PaymentService>.Instance),
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
