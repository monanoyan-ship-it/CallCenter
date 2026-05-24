using CallCenter.Api.Services;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CallCenter.Tests.Services;

public sealed class PaymentServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new PaymentService(_db, null!, null!, NullLogger<PaymentService>.Instance);
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
}
