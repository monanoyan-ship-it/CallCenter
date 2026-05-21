using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Shared.Entities;
using Microsoft.Extensions.Configuration;

namespace CallCenter.Tests.Factories;

public class PortalFactoryPhotoTests
{
    [Fact]
    public async Task GetPersonnelPhotoUrlAsync_ReturnsExistingPhotoUrl()
    {
        var personnel = new CustomerPersonnel { Id = 7, CustomerId = 1, PhotoUrl = "personnel/1/7-old.jpg" };
        var personnelEs = Substitute.For<ICustomerPersonnelEntityService>();
        personnelEs.GetByIdWithUserAsync(7, 1).Returns(personnel);
        var factory = CreateFactory(personnelEs);

        var result = await factory.GetPersonnelPhotoUrlAsync(1, 7);

        result.Success.Should().BeTrue();
        result.PhotoUrl.Should().Be("personnel/1/7-old.jpg");
    }

    [Fact]
    public async Task GetPersonnelPhotoUrlAsync_RejectsPersonnelOutsideCustomer()
    {
        var personnelEs = Substitute.For<ICustomerPersonnelEntityService>();
        personnelEs.GetByIdWithUserAsync(7, 1).Returns((CustomerPersonnel?)null);
        var factory = CreateFactory(personnelEs);

        var result = await factory.GetPersonnelPhotoUrlAsync(1, 7);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Personel bulunamadı.");
    }

    [Fact]
    public async Task UpdatePersonnelPhotoAsync_UpdatesPhotoAndSaves()
    {
        var personnel = new CustomerPersonnel { Id = 7, CustomerId = 1, PhotoUrl = "personnel/1/7-old.jpg" };
        var personnelEs = Substitute.For<ICustomerPersonnelEntityService>();
        personnelEs.GetByIdWithUserAsync(7, 1).Returns(personnel);
        var uow = Substitute.For<IUnitOfWork>();
        var factory = CreateFactory(personnelEs, uow);

        var result = await factory.UpdatePersonnelPhotoAsync(1, 7, "personnel/1/7-new.jpg");

        result.Success.Should().BeTrue();
        personnel.PhotoUrl.Should().Be("personnel/1/7-new.jpg");
        await uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UpdatePersonnelPhotoAsync_DoesNotSaveWhenPersonnelMissing()
    {
        var personnelEs = Substitute.For<ICustomerPersonnelEntityService>();
        personnelEs.GetByIdWithUserAsync(7, 1).Returns((CustomerPersonnel?)null);
        var uow = Substitute.For<IUnitOfWork>();
        var factory = CreateFactory(personnelEs, uow);

        var result = await factory.UpdatePersonnelPhotoAsync(1, 7, "personnel/1/7-new.jpg");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Personel bulunamadı.");
        await uow.DidNotReceive().SaveChangesAsync();
    }

    private static PortalFactory CreateFactory(
        ICustomerPersonnelEntityService personnelEs,
        IUnitOfWork? uow = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "portal-photo-tests"
            })
            .Build();

        return new PortalFactory(
            Substitute.For<ICustomerEntityService>(),
            personnelEs,
            Substitute.For<ICustomerPortalModuleEntityService>(),
            Substitute.For<ISipAccountEntityService>(),
            Substitute.For<IUserEntityService>(),
            Substitute.For<ICallRecordEntityService>(),
            Substitute.For<IPasswordPolicyFactory>(),
            new AesEncryptionService(config),
            Substitute.For<ISlnPersonnelSkillEntityService>(),
            Substitute.For<ISlnPersonnelCommissionEntityService>(),
            Substitute.For<ISlnPersonnelShiftEntityService>(),
            Substitute.For<ISlnPersonnelLeaveEntityService>(),
            Substitute.For<ISlnPersonnelTimesheetEntityService>(),
            Substitute.For<ISlnPayrollEntityService>(),
            Substitute.For<ISlnAdvanceEntityService>(),
            Substitute.For<ISlnInvoiceItemEntityService>(),
            uow ?? Substitute.For<IUnitOfWork>());
    }
}
