using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Tests.Helpers;

namespace CallCenter.Tests.Factories;

public class BillingFactoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly BillingFactory _sut;

    public BillingFactoryTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new BillingFactory(
            new BillingPeriodEntityService(_db),
            new CustomerEntityService(_db),
            new CustomerPersonnelEntityService(_db),
            new CustomerServiceSubscriptionEntityService(_db),
            new ServiceBillingItemEntityService(_db),
            new CustomerProductEntityService(_db),
            new CustomerBillingPeriodModuleLineEntityService(_db),
            Substitute.For<ISubscriptionFactory>(),
            new UnitOfWork(_db));
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

    private void AddCustomer(int id)
    {
        _db.Customers.Add(new Customer
        {
            Id = id,
            Uid = Guid.NewGuid(),
            Name = $"Customer {id}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
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
}
