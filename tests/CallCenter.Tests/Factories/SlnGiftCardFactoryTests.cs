using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Tests.Factories;

public class SlnGiftCardFactoryTests : IDisposable
{
    private readonly AppDbContext _db;

    public SlnGiftCardFactoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
    }

    [Fact]
    public async Task GetGiftCardsAsync_WhenBranchScoped_ReturnsGlobalAndMatchingBranchCards()
    {
        SeedBranchCards();
        var factory = CreateFactory();

        var cards = await factory.GetGiftCardsAsync(customerId: 1, branchId: 10);

        cards.Select(c => c.Code).Should().BeEquivalentTo("GC-GLOBAL", "GC-B10");
        cards.Should().Contain(c => c.Code == "GC-B10" && c.BranchId == 10 && c.BranchName == "Kadikoy");
        cards.Should().NotContain(c => c.Code == "GC-B11");
    }

    [Fact]
    public async Task GetGiftCardByCodeAsync_WhenBranchScoped_DoesNotReturnOtherBranchCard()
    {
        SeedBranchCards();
        var factory = CreateFactory();

        var card = await factory.GetGiftCardByCodeAsync("GC-B11", customerId: 1, branchId: 10);

        card.Should().BeNull();
    }

    [Fact]
    public async Task RedeemGiftCardAsync_WhenBranchScoped_DoesNotRedeemOtherBranchCard()
    {
        SeedBranchCards();
        var factory = CreateFactory();

        var result = await factory.RedeemGiftCardAsync(new SlnGiftCardRedeemDto
        {
            Code = "GC-B11",
            Amount = 25
        }, customerId: 1, branchId: 10);

        result.Success.Should().BeFalse();
        var otherBranchCard = await _db.SlnGiftCards.SingleAsync(c => c.Code == "GC-B11");
        otherBranchCard.RemainingBalance.Should().Be(100);
    }

    private void SeedBranchCards()
    {
        _db.SlnBranches.AddRange(
            new SlnBranch { Id = 10, CustomerId = 1, Name = "Kadikoy", IsActive = true },
            new SlnBranch { Id = 11, CustomerId = 1, Name = "Besiktas", IsActive = true });

        _db.SlnGiftCards.AddRange(
            new SlnGiftCard
            {
                Id = 1,
                CustomerId = 1,
                Code = "GC-GLOBAL",
                OriginalAmount = 100,
                RemainingBalance = 100,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-3)
            },
            new SlnGiftCard
            {
                Id = 2,
                CustomerId = 1,
                BranchId = 10,
                Code = "GC-B10",
                OriginalAmount = 100,
                RemainingBalance = 100,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-2)
            },
            new SlnGiftCard
            {
                Id = 3,
                CustomerId = 1,
                BranchId = 11,
                Code = "GC-B11",
                OriginalAmount = 100,
                RemainingBalance = 100,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-1)
            });

        _db.SaveChanges();
    }

    private SlnGiftCardFactory CreateFactory()
        => new(
            new SlnGiftCardEntityService(_db),
            new SlnGiftCardTransactionEntityService(_db),
            new SlnInvoiceEntityService(_db),
            new SlnInvoiceItemEntityService(_db),
            new SlnCashRegisterEntityService(_db),
            new SlnCashTransactionEntityService(_db),
            new UnitOfWork(_db));

    public void Dispose()
    {
        _db.Dispose();
    }
}
