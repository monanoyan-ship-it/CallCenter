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

    private SlnServiceFactory CreateFactory()
        => new(
            new SlnServiceCategoryEntityService(_db),
            new SlnServiceEntityService(_db),
            new SlnResourceEntityService(_db),
            new SlnServiceResourceRequirementEntityService(_db),
            new SlnServiceComboEntityService(_db),
            new SlnServiceComboItemEntityService(_db),
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
}
