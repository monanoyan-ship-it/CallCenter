using CallCenter.Api.Services;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Services.Payment;
using CallCenter.Api.Controllers;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CallCenter.Tests.Services;

public sealed class PaymentServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ISubscriptionFactory _subscriptionFactory;
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _subscriptionFactory = Substitute.For<ISubscriptionFactory>();
        _sut = CreatePaymentService(_subscriptionFactory);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetTransactionsAsync_WithOrganizationPaymentTypes_ExcludesSalonCustomerPayments()
    {
        _db.Customers.Add(new Customer
        {
            Id = 1,
            Uid = Guid.NewGuid(),
            Name = "Salon Firma",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        AddTransaction(1, PaymentTypes.Ids.PlatformAbonelik, DateTime.UtcNow.AddMinutes(-1));
        AddTransaction(1, PaymentTypes.Ids.ModulSatinAlma, DateTime.UtcNow.AddMinutes(-2));
        AddTransaction(1, PaymentTypes.Ids.RandevuOnOdemesi, DateTime.UtcNow.AddMinutes(-3));
        AddTransaction(1, PaymentTypes.Ids.SalonAdisyon, DateTime.UtcNow.AddMinutes(-4));
        AddTransaction(1, PaymentTypes.Ids.UyelikOdemesi, DateTime.UtcNow.AddMinutes(-5));
        await _db.SaveChangesAsync();

        var result = await _sut.GetTransactionsAsync(
            customerId: 1,
            paymentTypeIds: new[] { PaymentTypes.Ids.PlatformAbonelik, PaymentTypes.Ids.ModulSatinAlma });

        result.Select(t => t.PaymentTypeId).Should().Equal(
            PaymentTypes.Ids.PlatformAbonelik,
            PaymentTypes.Ids.ModulSatinAlma);
    }

    [Fact]
    public async Task CompleteCheckoutAsync_WhenAlreadySuccessful_ReturnsSuccessWithoutGateway()
    {
        var uid = Guid.NewGuid();
        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            Uid = uid,
            PaymentTypeId = PaymentTypes.Ids.ModulSatinAlma,
            PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti,
            StatusId = PaymentStatuses.Ids.Basarili,
            Amount = 100m,
            ProviderTransactionId = "checkout-token",
            ProviderPaymentId = "28157248",
            CompletedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _sut.CompleteCheckoutAsync("checkout-token");

        result.Success.Should().BeTrue();
        result.TransactionUid.Should().Be(uid);
        result.ProviderTransactionId.Should().Be("checkout-token");
    }

    [Fact]
    public async Task HandleIyzicoWebhookAsync_WhenSameEventArrivesTwice_DoesNotAppendDuplicateEvent()
    {
        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            PaymentTypeId = PaymentTypes.Ids.SalonAdisyon,
            PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti,
            StatusId = PaymentStatuses.Ids.Basarili,
            Amount = 100m,
            ProviderPaymentId = "28157248"
        });
        await _db.SaveChangesAsync();

        var payload = new PaymentController.IyzicoWebhookPayload
        {
            IyziEventType = "MARKETPLACE_SETTLEMENT_RECEIVED",
            IyziEventTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            IyziPaymentId = "28157248",
            PaymentConversationId = Guid.NewGuid().ToString("N"),
            IyziReferenceCode = "event-123",
            Status = "SUCCESS",
            Amount = 100m
        };

        var first = await _sut.HandleIyzicoWebhookAsync(payload);
        var second = await _sut.HandleIyzicoWebhookAsync(payload);
        var tx = _db.PaymentTransactions.Single();

        first.Handled.Should().BeTrue();
        second.Handled.Should().BeTrue();
        second.Message.Should().Contain("Duplicate");
        tx.Notes.Should().Contain("IyzicoWebhookId:");
        tx.Notes!.Split("IyzicoWebhookId:", StringSplitOptions.None).Length.Should().Be(2);
    }

    [Fact]
    public async Task InitSubscriptionCheckoutAsync_CancelsPendingUnifiedCheckout_ForSameCustomer()
    {
        var customer = AddCustomer(1);
        var period = new CustomerBillingPeriod
        {
            Id = 91,
            CustomerId = customer.Id,
            BillingKindId = CustomerBillingKinds.SalonPlatform,
            Year = 2026,
            Month = 5,
            PeriodStartDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEndDate = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            Amount = 1700m,
            StatusId = BillingPeriodStatuses.Ids.Draft
        };
        _db.CustomerBillingPeriods.Add(period);
        _db.CustomerSubscriptions.Add(new CustomerSubscription
        {
            CustomerId = customer.Id,
            Customer = customer,
            PlanId = 1,
            StartDate = period.PeriodStartDate,
            MonthlyPrice = 1700m,
            PeriodPrice = 1700m,
            BillingDay = 1,
            StatusId = 1
        });
        AddInvalidPaymentConfig();
        var unified = AddPendingUnifiedCheckout(customer.Id, period.Id);
        _subscriptionFactory.TryResolveSalonSubscriptionPaymentAsync(customer.Id)
            .Returns(Task.FromResult<(CustomerBillingPeriod Period, decimal PayAmount)?>((period, 1700m)));
        await _db.SaveChangesAsync();

        var result = await _sut.InitSubscriptionCheckoutAsync(customer.Id, "https://example.test/callback");

        result.Success.Should().BeFalse();
        unified.StatusId.Should().Be(PaymentStatuses.Ids.Iptal);
        unified.Notes.Should().Contain("CheckoutSuperseded");
    }

    [Fact]
    public async Task InitPackageCheckoutAsync_CancelsPendingUnifiedCheckout_ForSameCustomer()
    {
        var customer = AddCustomer(1);
        AddInvalidPaymentConfig();
        var unified = AddPendingUnifiedCheckout(customer.Id, billingPeriodId: 92);
        _subscriptionFactory.GetNextSalonAccrualUtcAsync(customer.Id)
            .Returns(Task.FromResult<DateTime?>(DateTime.UtcNow.AddDays(30)));
        await _db.SaveChangesAsync();

        var result = await _sut.InitPackageCheckoutAsync(
            customer.Id,
            SalonModuleGroups.Ids.LoyaltyMarketing,
            "https://example.test/callback");

        result.Success.Should().BeFalse();
        unified.StatusId.Should().Be(PaymentStatuses.Ids.Iptal);
        unified.Notes.Should().Contain("CheckoutSuperseded");
    }

    private void AddTransaction(int customerId, int paymentTypeId, DateTime createdAt)
    {
        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            CustomerId = customerId,
            PaymentTypeId = paymentTypeId,
            PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti,
            StatusId = PaymentStatuses.Ids.Basarili,
            Amount = 100m,
            Currency = "TRY",
            CreatedAt = createdAt,
            CompletedAt = createdAt.AddMinutes(1)
        });
    }

    private PaymentService CreatePaymentService(ISubscriptionFactory subscriptionFactory)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "unit-test-encryption-key"
            })
            .Build();
        var gatewayFactory = new PaymentGatewayFactory(new AesEncryptionService(config));

        return new PaymentService(_db, gatewayFactory, subscriptionFactory, NullLogger<PaymentService>.Instance);
    }

    private Customer AddCustomer(int id)
    {
        var customer = new Customer
        {
            Id = id,
            Uid = Guid.NewGuid(),
            Name = $"Customer {id}",
            Email = $"customer{id}@example.test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(customer);
        return customer;
    }

    private void AddInvalidPaymentConfig()
    {
        _db.PlatformPaymentConfigs.Add(new PlatformPaymentConfig
        {
            ProviderTypeId = 999,
            EncryptedCredentials = "{}",
            IsActive = true,
            IsSandbox = true
        });
    }

    private PaymentTransaction AddPendingUnifiedCheckout(int customerId, int billingPeriodId)
    {
        var tx = new PaymentTransaction
        {
            CustomerId = customerId,
            PaymentTypeId = PaymentTypes.Ids.TopluTahakkuk,
            PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti,
            StatusId = PaymentStatuses.Ids.Beklemede,
            Amount = 1700m,
            Currency = "TRY",
            Provider = "Iyzico"
        };
        tx.Lines.Add(new PaymentTransactionLine
        {
            BillingPeriodId = billingPeriodId,
            BillingKindId = CustomerBillingKinds.SalonPlatform,
            Description = "Salon platform 05/2026",
            Amount = 1700m,
            Currency = "TRY"
        });
        _db.PaymentTransactions.Add(tx);
        return tx;
    }
}
