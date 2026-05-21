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

    private async Task SeedServiceAsync(bool isActive)
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

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }
}
