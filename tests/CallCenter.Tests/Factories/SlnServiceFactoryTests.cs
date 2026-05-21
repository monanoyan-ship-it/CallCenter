using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CallCenter.Tests.Factories;

public class SlnServiceFactoryTests : IDisposable
{
    private readonly AppDbContext _db;

    public SlnServiceFactoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task UpdateServiceAsync_PreservesInactiveState_WhenIsActiveIsOmitted()
    {
        await SeedServiceAsync(isActive: false);
        var factory = CreateFactory();

        var result = await factory.UpdateServiceAsync(10, CreateDto(), isActive: null, customerId: 1);
        var service = await _db.SlnServices.AsNoTracking().SingleAsync(s => s.Id == 10);

        result.Success.Should().BeTrue();
        service.IsActive.Should().BeFalse();
        service.Name.Should().Be("Updated service");
        service.TaxRate.Should().Be(8m);
        service.SortOrder.Should().Be(4);
    }

    [Fact]
    public async Task UpdateServiceAsync_AppliesExplicitActiveState()
    {
        await SeedServiceAsync(isActive: false);
        var factory = CreateFactory();

        var result = await factory.UpdateServiceAsync(10, CreateDto(), isActive: true, customerId: 1);
        var service = await _db.SlnServices.AsNoTracking().SingleAsync(s => s.Id == 10);

        result.Success.Should().BeTrue();
        service.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateServiceAsync_PreservesResourceRequirements_WhenSyncIsDisabled()
    {
        await SeedServiceAsync(isActive: true, includeRequirement: true);
        var factory = CreateFactory();

        var result = await factory.UpdateServiceAsync(10, CreateDto(), isActive: null, customerId: 1, syncResourceRequirements: false);
        var requirements = await _db.SlnServiceResourceRequirements.AsNoTracking()
            .Where(r => r.ServiceId == 10)
            .ToListAsync();

        result.Success.Should().BeTrue();
        requirements.Should().ContainSingle();
        requirements.Single().ResourceId.Should().Be(30);
        requirements.Single().QuantityRequired.Should().Be(2);
    }

    [Fact]
    public async Task UpdateServiceAsync_ReplacesResourceRequirements_WhenSyncIsEnabled()
    {
        await SeedServiceAsync(isActive: true, includeRequirement: true);
        var dto = CreateDto();
        dto.ResourceRequirements = [];
        var factory = CreateFactory();

        var result = await factory.UpdateServiceAsync(10, dto, isActive: null, customerId: 1, syncResourceRequirements: true);
        var requirementCount = await _db.SlnServiceResourceRequirements.AsNoTracking()
            .CountAsync(r => r.ServiceId == 10);

        result.Success.Should().BeTrue();
        requirementCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateServiceAsync_PersistsTaxRateAndSortOrder()
    {
        await SeedServiceAsync(isActive: true);
        var dto = CreateDto();
        dto.TaxRate = 18m;
        dto.SortOrder = 7;
        var factory = CreateFactory();

        var result = await factory.CreateServiceAsync(dto, customerId: 1);
        var service = await _db.SlnServices.AsNoTracking().SingleAsync(s => s.Id == result.Service!.Id);

        result.Service.Should().NotBeNull();
        result.Service!.TaxRate.Should().Be(18m);
        result.Service.SortOrder.Should().Be(7);
        service.TaxRate.Should().Be(18m);
        service.SortOrder.Should().Be(7);
    }

    [Fact]
    public async Task UpdateServiceAsync_UpdatesTaxRateAndSortOrder()
    {
        await SeedServiceAsync(isActive: true);
        var dto = CreateDto();
        dto.TaxRate = 18m;
        dto.SortOrder = 7;
        var factory = CreateFactory();

        var result = await factory.UpdateServiceAsync(10, dto, isActive: null, customerId: 1);
        var service = await _db.SlnServices.AsNoTracking().SingleAsync(s => s.Id == 10);

        result.Success.Should().BeTrue();
        service.TaxRate.Should().Be(18m);
        service.SortOrder.Should().Be(7);
    }

    [Fact]
    public async Task CreateServiceAsync_RejectsCategoryOutsideCustomer()
    {
        await SeedServiceAsync(isActive: true);
        var dto = CreateDto();
        dto.CategoryId = 999;
        var factory = CreateFactory();

        var result = await factory.CreateServiceAsync(dto, customerId: 1);

        result.Service.Should().BeNull();
        result.Error.Should().Be("Kategori bulunamadi");
        (await _db.SlnServices.AsNoTracking().CountAsync(s => s.Name == "Updated service")).Should().Be(0);
    }

    [Fact]
    public async Task UpdateServiceAsync_RejectsCategoryOutsideCustomerWithoutMutating()
    {
        await SeedServiceAsync(isActive: true);
        var dto = CreateDto();
        dto.CategoryId = 999;
        var factory = CreateFactory();

        var result = await factory.UpdateServiceAsync(10, dto, isActive: null, customerId: 1);
        var service = await _db.SlnServices.AsNoTracking().SingleAsync(s => s.Id == 10);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Kategori bulunamadi");
        service.CategoryId.Should().Be(20);
        service.Name.Should().Be("Existing service");
    }

    [Fact]
    public async Task UpdateServiceAsync_RejectsInvalidTaxRateWithoutMutating()
    {
        await SeedServiceAsync(isActive: true);
        var dto = CreateDto();
        dto.TaxRate = 101m;
        var factory = CreateFactory();

        var result = await factory.UpdateServiceAsync(10, dto, isActive: null, customerId: 1);
        var service = await _db.SlnServices.AsNoTracking().SingleAsync(s => s.Id == 10);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("KDV orani 0 ile 100 arasinda olmali");
        service.TaxRate.Should().Be(8m);
    }

    [Fact]
    public async Task UpdateServiceAsync_RejectsInvalidSortOrderWithoutMutating()
    {
        await SeedServiceAsync(isActive: true);
        var dto = CreateDto();
        dto.SortOrder = -1;
        var factory = CreateFactory();

        var result = await factory.UpdateServiceAsync(10, dto, isActive: null, customerId: 1);
        var service = await _db.SlnServices.AsNoTracking().SingleAsync(s => s.Id == 10);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Sira 0 veya daha buyuk olmali");
        service.SortOrder.Should().Be(4);
    }

    [Fact]
    public async Task CreateServiceAsync_RejectsParentOutsideCustomer()
    {
        await SeedServiceAsync(isActive: true);
        await SeedForeignServiceAsync();
        var dto = CreateDto();
        dto.ParentServiceId = 11;
        var factory = CreateFactory();

        var result = await factory.CreateServiceAsync(dto, customerId: 1);

        result.Service.Should().BeNull();
        result.Error.Should().Be("Ust hizmet bulunamadi");
        (await _db.SlnServices.AsNoTracking().CountAsync(s => s.Name == "Updated service")).Should().Be(0);
    }

    [Fact]
    public async Task UpdateServiceAsync_RejectsSelfParentWithoutMutating()
    {
        await SeedServiceAsync(isActive: true);
        var dto = CreateDto();
        dto.ParentServiceId = 10;
        var factory = CreateFactory();

        var result = await factory.UpdateServiceAsync(10, dto, isActive: null, customerId: 1);
        var service = await _db.SlnServices.AsNoTracking().SingleAsync(s => s.Id == 10);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Hizmet kendisinin ust hizmeti olamaz");
        service.ParentServiceId.Should().BeNull();
        service.Name.Should().Be("Existing service");
    }

    [Fact]
    public async Task UpdateServiceAsync_RejectsResourceOutsideCustomerWithoutClearingExistingRequirements()
    {
        await SeedServiceAsync(isActive: true, includeRequirement: true);
        await SeedForeignResourceAsync();
        var dto = CreateDto();
        dto.ResourceRequirements =
        [
            new SlnServiceResourceRequirementCreateDto
            {
                ResourceId = 32,
                QuantityRequired = 1
            }
        ];
        var factory = CreateFactory();

        var result = await factory.UpdateServiceAsync(10, dto, isActive: null, customerId: 1);
        var service = await _db.SlnServices.AsNoTracking().SingleAsync(s => s.Id == 10);
        var requirements = await _db.SlnServiceResourceRequirements.AsNoTracking().Where(r => r.ServiceId == 10).ToListAsync();

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Kaynak bulunamadi");
        service.Name.Should().Be("Existing service");
        requirements.Should().ContainSingle();
        requirements.Single().ResourceId.Should().Be(30);
    }

    [Fact]
    public async Task CreateComboAsync_RejectsEmptyItems()
    {
        await SeedServiceAsync(isActive: true);
        var factory = CreateFactory();

        var result = await factory.CreateComboAsync(new SlnServiceComboCreateDto
        {
            Name = "Combo",
            Price = 500m,
            Items = []
        }, customerId: 1);

        result.Combo.Should().BeNull();
        result.Error.Should().Be("Combo icin en az bir hizmet secin");
        (await _db.SlnServiceCombos.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateComboAsync_RejectsInvalidServiceIdWithoutCreatingCombo()
    {
        await SeedServiceAsync(isActive: true);
        var factory = CreateFactory();

        var result = await factory.CreateComboAsync(new SlnServiceComboCreateDto
        {
            Name = "Combo",
            Price = 500m,
            Items = [new SlnServiceComboItemCreateDto { ServiceId = 999, SortOrder = 1 }]
        }, customerId: 1);

        result.Combo.Should().BeNull();
        result.Error.Should().Be("Hizmet bulunamadi");
        (await _db.SlnServiceCombos.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task UpdateComboAsync_RejectsInvalidServiceIdWithoutClearingExistingItems()
    {
        await SeedServiceAsync(isActive: true);
        await SeedComboAsync();
        var factory = CreateFactory();

        var result = await factory.UpdateComboAsync(60, new SlnServiceComboCreateDto
        {
            Name = "Updated combo",
            Price = 750m,
            Items = [new SlnServiceComboItemCreateDto { ServiceId = 999, SortOrder = 1 }]
        }, customerId: 1);
        var combo = await _db.SlnServiceCombos.AsNoTracking().SingleAsync(c => c.Id == 60);
        var items = await _db.SlnServiceComboItems.AsNoTracking().Where(i => i.ComboId == 60).ToListAsync();

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Hizmet bulunamadi");
        combo.Name.Should().Be("Existing combo");
        items.Should().ContainSingle();
        items.Single().ServiceId.Should().Be(10);
    }

    [Fact]
    public async Task DeleteComboAsync_RejectsHistoricalAppointmentUsage()
    {
        await SeedServiceAsync(isActive: true);
        await SeedComboAsync();
        _db.SlnAppointments.Add(new SlnAppointment
        {
            Id = 70,
            CustomerId = 1,
            ComboId = 60,
            StartTime = new DateTime(2026, 5, 21, 9, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 5, 21, 10, 0, 0, DateTimeKind.Utc)
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        var factory = CreateFactory();

        var result = await factory.DeleteComboAsync(60, customerId: 1);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Bu combo randevularda kullaniliyor");
        (await _db.SlnServiceCombos.AsNoTracking().AnyAsync(c => c.Id == 60)).Should().BeTrue();
    }

    [Fact]
    public async Task GetResourcesAsync_FiltersByBranchScopeAndKeepsGlobalResources()
    {
        await SeedResourcesAsync();
        var factory = CreateFactory();

        var resources = await factory.GetResourcesAsync(customerId: 1, branchScopeId: 40);

        resources.Select(r => r.Id).Should().BeEquivalentTo(new[] { 30, 31 });
        resources.Should().NotContain(r => r.Id == 32);
    }

    [Fact]
    public async Task CreateResourceAsync_ForcesBranchScope()
    {
        await SeedResourcesAsync(includeResources: false);
        var factory = CreateFactory();

        var created = await factory.CreateResourceAsync(new SlnResourceCreateDto
        {
            BranchId = 42,
            Name = "Scoped room",
            Quantity = 2,
            IsActive = true
        }, customerId: 1, branchScopeId: 40);
        var saved = await _db.SlnResources.AsNoTracking().SingleAsync(r => r.Id == created.Id);

        saved.BranchId.Should().Be(40);
    }

    [Fact]
    public async Task UpdateResourceAsync_RejectsOutsideBranchScope()
    {
        await SeedResourcesAsync();
        var factory = CreateFactory();

        var otherBranch = await factory.UpdateResourceAsync(32, new SlnResourceCreateDto
        {
            BranchId = 40,
            Name = "Other branch edit",
            Quantity = 1,
            IsActive = true
        }, customerId: 1, branchScopeId: 40);
        var global = await factory.UpdateResourceAsync(31, new SlnResourceCreateDto
        {
            BranchId = 40,
            Name = "Global edit",
            Quantity = 1,
            IsActive = true
        }, customerId: 1, branchScopeId: 40);

        otherBranch.Success.Should().BeFalse();
        otherBranch.Error.Should().Be("Bu kaynak icin yetkiniz yok");
        global.Success.Should().BeFalse();
        global.Error.Should().Be("Bu kaynak icin yetkiniz yok");
        (await _db.SlnResources.AsNoTracking().SingleAsync(r => r.Id == 32)).Name.Should().Be("Other branch room");
        (await _db.SlnResources.AsNoTracking().SingleAsync(r => r.Id == 31)).Name.Should().Be("Global room");
    }

    [Fact]
    public async Task DeleteResourceAsync_RejectsOutsideBranchScope()
    {
        await SeedResourcesAsync();
        var factory = CreateFactory();

        var otherBranch = await factory.DeleteResourceAsync(32, customerId: 1, branchScopeId: 40);
        var global = await factory.DeleteResourceAsync(31, customerId: 1, branchScopeId: 40);

        otherBranch.Success.Should().BeFalse();
        otherBranch.Error.Should().Be("Bu kaynak icin yetkiniz yok");
        global.Success.Should().BeFalse();
        global.Error.Should().Be("Bu kaynak icin yetkiniz yok");
        (await _db.SlnResources.AsNoTracking().CountAsync()).Should().Be(3);
    }

    private SlnServiceFactory CreateFactory()
        => new(
            new SlnServiceCategoryEntityService(_db),
            new SlnServiceEntityService(_db),
            new SlnResourceEntityService(_db),
            new SlnServiceResourceRequirementEntityService(_db),
            new SlnServiceComboEntityService(_db),
            new SlnServiceComboItemEntityService(_db),
            new SlnAppointmentEntityService(_db),
            new SlnBranchEntityService(_db),
            new UnitOfWork(_db),
            NullLogger<SlnServiceFactory>.Instance);

    private static SlnServiceCreateDto CreateDto()
        => new()
        {
            CategoryId = 20,
            Name = "Updated service",
            DurationMinutes = 45,
            BufferBeforeMinutes = 5,
            BufferAfterMinutes = 10,
            ProcessingMinutes = 30,
            Price = 250m,
            RequiresConsultation = true,
            RequiresPatchTest = true,
            ResourceRequirements = []
        };

    private async Task SeedServiceAsync(bool isActive, bool includeRequirement = false)
    {
        _db.Customers.Add(new Customer
        {
            Id = 1,
            Uid = Guid.NewGuid(),
            Name = "Salon",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _db.SlnServiceCategories.Add(new SlnServiceCategory
        {
            Id = 20,
            CustomerId = 1,
            Name = "Category",
            IsActive = true
        });
        _db.SlnServices.Add(new SlnService
        {
            Id = 10,
            CustomerId = 1,
            CategoryId = 20,
            Name = "Existing service",
            DurationMinutes = 30,
            Price = 100m,
            TaxRate = 8m,
            SortOrder = 4,
            IsActive = isActive
        });
        if (includeRequirement)
        {
            _db.SlnBranches.Add(new SlnBranch
            {
                Id = 40,
                CustomerId = 1,
                Name = "Merkez",
                IsActive = true
            });
            _db.SlnResources.Add(new SlnResource
            {
                Id = 30,
                CustomerId = 1,
                BranchId = 40,
                Name = "Oda",
                Quantity = 1,
                IsActive = true
            });
            _db.SlnServiceResourceRequirements.Add(new SlnServiceResourceRequirement
            {
                Id = 50,
                ServiceId = 10,
                ResourceId = 30,
                QuantityRequired = 2
            });
        }

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    private async Task SeedForeignServiceAsync()
    {
        _db.Customers.Add(new Customer
        {
            Id = 2,
            Uid = Guid.NewGuid(),
            Name = "Other salon",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _db.SlnServiceCategories.Add(new SlnServiceCategory
        {
            Id = 21,
            CustomerId = 2,
            Name = "Other category",
            IsActive = true
        });
        _db.SlnServices.Add(new SlnService
        {
            Id = 11,
            CustomerId = 2,
            CategoryId = 21,
            Name = "Other service",
            DurationMinutes = 30,
            Price = 100m,
            IsActive = true
        });

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    private async Task SeedForeignResourceAsync()
    {
        _db.Customers.Add(new Customer
        {
            Id = 2,
            Uid = Guid.NewGuid(),
            Name = "Other salon",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _db.SlnResources.Add(new SlnResource
        {
            Id = 32,
            CustomerId = 2,
            Name = "Other salon room",
            Quantity = 1,
            IsActive = true
        });

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    private async Task SeedComboAsync()
    {
        _db.SlnServiceCombos.Add(new SlnServiceCombo
        {
            Id = 60,
            CustomerId = 1,
            Name = "Existing combo",
            Price = 500m,
            IsActive = true
        });
        _db.SlnServiceComboItems.Add(new SlnServiceComboItem
        {
            Id = 61,
            ComboId = 60,
            ServiceId = 10,
            SortOrder = 1
        });

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    private async Task SeedResourcesAsync(bool includeResources = true)
    {
        _db.Customers.Add(new Customer
        {
            Id = 1,
            Uid = Guid.NewGuid(),
            Name = "Salon",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _db.SlnBranches.AddRange(
            new SlnBranch
            {
                Id = 40,
                CustomerId = 1,
                Name = "Merkez",
                IsActive = true
            },
            new SlnBranch
            {
                Id = 42,
                CustomerId = 1,
                Name = "Sube",
                IsActive = true
            });

        if (includeResources)
        {
            _db.SlnResources.AddRange(
                new SlnResource
                {
                    Id = 30,
                    CustomerId = 1,
                    BranchId = 40,
                    Name = "Own branch room",
                    Quantity = 1,
                    IsActive = true,
                    SortOrder = 2
                },
                new SlnResource
                {
                    Id = 31,
                    CustomerId = 1,
                    BranchId = null,
                    Name = "Global room",
                    Quantity = 1,
                    IsActive = true,
                    SortOrder = 1
                },
                new SlnResource
                {
                    Id = 32,
                    CustomerId = 1,
                    BranchId = 42,
                    Name = "Other branch room",
                    Quantity = 1,
                    IsActive = true,
                    SortOrder = 3
                });
        }

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }
}
