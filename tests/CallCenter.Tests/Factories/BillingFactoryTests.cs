using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Tests.Helpers;

namespace CallCenter.Tests.Factories;

public class BillingFactoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly BillingFactory _sut;
    private int _nextUserId = 1000;
    private int _nextPersonnelId = 2000;

    public BillingFactoryTests()
    {
        _db = TestDbContextFactory.Create();
        var uow = new UnitOfWork(_db);
        var servicePricingFactory = new ServicePricingFactory(
            new ServicePricingPeriodEntityService(_db),
            new ServicePricingItemEntityService(_db),
            new ModulePricingEntityService(_db),
            uow);
        _sut = new BillingFactory(
            new BillingPeriodEntityService(_db),
            new CustomerEntityService(_db),
            new CustomerPersonnelEntityService(_db),
            new CustomerServiceSubscriptionEntityService(_db),
            new ServiceBillingItemEntityService(_db),
            new CustomerProductEntityService(_db),
            new CustomerBillingPeriodModuleLineEntityService(_db),
            new PaymentTransactionEntityService(_db),
            Substitute.For<ISubscriptionFactory>(),
            servicePricingFactory,
            uow);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task IsCustomerBlockedByBillingAsync_SalonOnlyCustomer_IgnoresLegacyCallCenterPeriod()
    {
        AddCustomer(1);
        _db.CustomerProducts.Add(new CustomerProduct
        {
            CustomerId = 1,
            ProductTypeId = ProductTypes.Ids.Salon,
            MonthlyPrice = 100m,
            IsActive = true
        });
        AddOverduePeriod(1, amount: 100m, serviceAmount: 0m);
        await _db.SaveChangesAsync();

        var result = await _sut.IsCustomerBlockedByBillingAsync(1);

        result.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task IsCustomerBlockedByBillingAsync_CallCenterCustomer_IgnoresZeroAmountPeriod()
    {
        AddCustomer(2);
        _db.CustomerProducts.Add(new CustomerProduct
        {
            CustomerId = 2,
            ProductTypeId = ProductTypes.Ids.CallCenter,
            MonthlyPrice = 100m,
            IsActive = true
        });
        AddOverduePeriod(2, amount: 0m, serviceAmount: 0m);
        await _db.SaveChangesAsync();

        var result = await _sut.IsCustomerBlockedByBillingAsync(2);

        result.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task IsCustomerBlockedByBillingAsync_CallCenterCustomer_BlocksPositiveOverduePeriod()
    {
        AddCustomer(3);
        _db.CustomerProducts.Add(new CustomerProduct
        {
            CustomerId = 3,
            ProductTypeId = ProductTypes.Ids.CallCenter,
            MonthlyPrice = 100m,
            IsActive = true
        });
        AddOverduePeriod(3, amount: 100m, serviceAmount: 0m);
        await _db.SaveChangesAsync();

        var result = await _sut.IsCustomerBlockedByBillingAsync(3);

        result.IsBlocked.Should().BeTrue();
        result.Reason.Should().Contain("Odenmemis donem");
    }

    [Fact]
    public async Task GenerateBulkAsync_CallCenterCustomer_BillsActiveOperatorsWithPricingPeriodUnitPrice()
    {
        AddCustomer(10, maxUsers: 5);
        AddActiveOperatorPrice(700m);
        _db.CustomerProducts.Add(new CustomerProduct
        {
            CustomerId = 10,
            ProductTypeId = ProductTypes.Ids.CallCenter,
            MonthlyPrice = 1400m,
            IsActive = true
        });
        AddOperator(10);
        AddOperator(10);
        AddOperator(10, isActive: false);
        await _db.SaveChangesAsync();

        var result = await _sut.GenerateBulkAsync(2026, 5);

        result.Created.Should().Be(1);
        var period = _db.CustomerBillingPeriods.Single(p => p.CustomerId == 10);
        period.UserCount.Should().Be(2);
        period.UnitPrice.Should().Be(700m);
        period.Amount.Should().Be(1400m);
    }

    [Fact]
    public async Task GenerateBulkAsync_SalonCrmCustomer_CreatesSalonCrmBillingOnly()
    {
        AddCustomer(15);
        _db.CustomerProducts.Add(new CustomerProduct
        {
            CustomerId = 15,
            ProductTypeId = ProductTypes.Ids.Salon,
            IsActive = true
        });
        _db.CustomerProducts.Add(new CustomerProduct
        {
            CustomerId = 15,
            ProductTypeId = ProductTypes.Ids.Crm,
            IsActive = true
        });
        _db.CustomerPortalModules.Add(new CustomerPortalModule
        {
            CustomerId = 15,
            ModuleId = CrmModules.Ids.SalonGiftCards,
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GenerateBulkAsync(2026, 5);

        result.Created.Should().Be(1);
        var period = _db.CustomerBillingPeriods.Single(p => p.CustomerId == 15);
        period.BillingKindId.Should().Be(CustomerBillingKinds.SalonCrm);
        period.UserCount.Should().Be(1);
        period.UnitPrice.Should().Be(1500m);
        period.Amount.Should().Be(1500m);
    }

    [Fact]
    public async Task CreateManualPeriodAsync_CallCenterCustomer_BillsActiveOperatorsIgnoringCustomerProductPrice()
    {
        AddCustomer(11, maxUsers: 5);
        AddActiveOperatorPrice(700m);
        _db.CustomerProducts.Add(new CustomerProduct
        {
            CustomerId = 11,
            ProductTypeId = ProductTypes.Ids.CallCenter,
            MonthlyPrice = 1400m,
            IsActive = true
        });
        AddOperator(11);
        AddOperator(11);
        await _db.SaveChangesAsync();

        var result = await _sut.CreateManualPeriodAsync(new BillingPeriodCreateDto
        {
            CustomerId = 11,
            PeriodStartDate = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc)
        });

        result.Success.Should().BeTrue(result.Error);
        var period = _db.CustomerBillingPeriods.Single(p => p.CustomerId == 11);
        period.UserCount.Should().Be(2);
        period.UnitPrice.Should().Be(700m);
        period.Amount.Should().Be(1400m);
    }

    [Fact]
    public async Task DeletePeriodAsync_DraftUnpaidPeriod_RemovesPeriod()
    {
        AddCustomer(12);
        var period = AddBillingPeriod(12, BillingPeriodStatuses.Ids.Draft);
        await _db.SaveChangesAsync();

        var result = await _sut.DeletePeriodAsync(period.Id);

        result.Success.Should().BeTrue(result.Error);
        _db.CustomerBillingPeriods.Should().NotContain(p => p.Id == period.Id);
    }

    [Fact]
    public async Task DeletePeriodAsync_InvoicedPeriod_ReturnsError()
    {
        AddCustomer(13);
        var period = AddBillingPeriod(13, BillingPeriodStatuses.Ids.Invoiced);
        await _db.SaveChangesAsync();

        var result = await _sut.DeletePeriodAsync(period.Id);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Sadece");
        _db.CustomerBillingPeriods.Should().Contain(p => p.Id == period.Id);
    }

    [Fact]
    public async Task DeletePeriodAsync_PeriodWithPaymentTransaction_ReturnsError()
    {
        AddCustomer(14);
        var period = AddBillingPeriod(14, BillingPeriodStatuses.Ids.Draft);
        await _db.SaveChangesAsync();
        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            BillingPeriodId = period.Id,
            CustomerId = 14,
            PaymentTypeId = PaymentTypes.Ids.PlatformAbonelik,
            PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti,
            Amount = 700m
        });
        await _db.SaveChangesAsync();

        var result = await _sut.DeletePeriodAsync(period.Id);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("odeme islemi");
        _db.CustomerBillingPeriods.Should().Contain(p => p.Id == period.Id);
    }

    private void AddCustomer(int id, int maxUsers = 0)
    {
        _db.Customers.Add(new Customer
        {
            Id = id,
            Uid = Guid.NewGuid(),
            Name = $"Customer {id}",
            IsActive = true,
            MaxUsers = maxUsers,
            CreatedAt = DateTime.UtcNow
        });
    }

    private void AddOperator(int customerId, bool isActive = true)
    {
        var userId = _nextUserId++;
        var personnelId = _nextPersonnelId++;
        _db.Users.Add(new User
        {
            Id = userId,
            Uid = Guid.NewGuid(),
            UserName = $"operator{userId}",
            FullName = $"Operator {userId}",
            Email = $"operator{userId}@test.local",
            PasswordHash = "$2a$11$test",
            RoleId = UserRoles.Ids.CustomerUser,
            StatusId = AgentStatuses.Ids.Offline,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        });
        _db.CustomerPersonnel.Add(new CustomerPersonnel
        {
            Id = personnelId,
            UserId = userId,
            CustomerId = customerId,
            Title = "Operator",
            CustomerRoleId = CustomerRoles.Ids.Operator,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        });
    }

    private void AddActiveOperatorPrice(decimal monthlyPrice)
    {
        var period = new ServicePricingPeriod
        {
            Name = "Aktif fiyat donemi",
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            StatusId = 1
        };
        period.Items.Add(new ServicePricingItem
        {
            ProductTypeId = PortalModules.ProductTypeId,
            ServiceId = ServicePricingFactory.CallCenterOperatorLicenseServiceId,
            ServiceName = ServicePricingFactory.CallCenterOperatorLicenseName,
            MonthlyPrice = monthlyPrice
        });
        _db.ServicePricingPeriods.Add(period);
    }

    private void AddOverduePeriod(int customerId, decimal amount, decimal serviceAmount)
    {
        var periodEnd = DateTime.UtcNow.Date.AddDays(-10);
        _db.CustomerBillingPeriods.Add(new CustomerBillingPeriod
        {
            CustomerId = customerId,
            BillingKindId = CustomerBillingKinds.CallCenter,
            Year = periodEnd.Year,
            Month = periodEnd.Month,
            PeriodStartDate = periodEnd.AddMonths(-1).AddDays(1),
            PeriodEndDate = periodEnd,
            UserCount = 1,
            UnitPrice = amount,
            Amount = amount,
            ServiceAmount = serviceAmount,
            StatusId = BillingPeriodStatuses.Ids.Draft,
            IsPaid = false
        });
    }

    private CustomerBillingPeriod AddBillingPeriod(int customerId, int statusId)
    {
        var period = new CustomerBillingPeriod
        {
            CustomerId = customerId,
            BillingKindId = CustomerBillingKinds.CallCenter,
            Year = 2026,
            Month = 5,
            PeriodStartDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEndDate = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            UserCount = 1,
            UnitPrice = 700m,
            Amount = 700m,
            ServiceAmount = 0m,
            StatusId = statusId,
            IsPaid = statusId == BillingPeriodStatuses.Ids.Paid,
            PaidAt = statusId == BillingPeriodStatuses.Ids.Paid ? DateTime.UtcNow : null,
            PaymentMethodId = statusId == BillingPeriodStatuses.Ids.Paid ? BillingPaymentMethods.Ids.Havale : null
        };
        _db.CustomerBillingPeriods.Add(period);
        return period;
    }
}
