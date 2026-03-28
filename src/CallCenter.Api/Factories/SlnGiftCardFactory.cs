using System.Security.Cryptography;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnGiftCardFactory : ISlnGiftCardFactory
{
    private readonly ISlnGiftCardEntityService _cards;
    private readonly ISlnGiftCardTransactionEntityService _transactions;
    private readonly IUnitOfWork _uow;

    public SlnGiftCardFactory(
        ISlnGiftCardEntityService cards,
        ISlnGiftCardTransactionEntityService transactions,
        IUnitOfWork uow)
    {
        _cards = cards;
        _transactions = transactions;
        _uow = uow;
    }

    public async Task<List<SlnGiftCardDto>> GetGiftCardsAsync(int customerId)
    {
        return await _cards.GetAllQueryable()
            .Where(g => g.CustomerId == customerId)
            .Include(g => g.SoldByPersonnel).ThenInclude(p => p!.User)
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => MapToDto(g))
            .ToListAsync();
    }

    public async Task<SlnGiftCardDto?> GetGiftCardAsync(int id, int customerId)
    {
        var card = await _cards.GetAllQueryable()
            .Include(g => g.SoldByPersonnel).ThenInclude(p => p!.User)
            .Include(g => g.Transactions)
            .FirstOrDefaultAsync(g => g.Id == id && g.CustomerId == customerId);
        return card != null ? MapToDto(card) : null;
    }

    public async Task<SlnGiftCardDto?> GetGiftCardByCodeAsync(string code, int customerId)
    {
        var card = await _cards.GetAllQueryable()
            .Include(g => g.SoldByPersonnel).ThenInclude(p => p!.User)
            .Include(g => g.Transactions)
            .FirstOrDefaultAsync(g => g.Code == code && g.CustomerId == customerId && g.IsActive);
        return card != null ? MapToDto(card) : null;
    }

    public async Task<SlnGiftCardDto> CreateGiftCardAsync(SlnGiftCardCreateDto dto, int userId, int customerId)
    {
        var card = new SlnGiftCard
        {
            CustomerId = customerId,
            Code = GenerateCode(),
            OriginalAmount = dto.Amount,
            RemainingBalance = dto.Amount,
            RecipientName = dto.RecipientName,
            RecipientPhone = dto.RecipientPhone,
            SenderName = dto.SenderName,
            Message = dto.Message,
            ExpiresAt = dto.ExpiresAt,
            SoldByPersonnelId = userId,
            IsActive = true
        };

        _cards.Add(card);
        await _uow.SaveChangesAsync();

        // Yukleme islemi
        _transactions.Add(new SlnGiftCardTransaction
        {
            GiftCardId = card.Id,
            TransactionTypeId = 1, // Yukleme
            Amount = dto.Amount,
            Description = "Hediye karti olusturuldu"
        });
        await _uow.SaveChangesAsync();

        return (await GetGiftCardAsync(card.Id, customerId))!;
    }

    public async Task<(bool Success, string? Error)> RedeemGiftCardAsync(SlnGiftCardRedeemDto dto, int customerId)
    {
        var card = await _cards.GetAllQueryable()
            .FirstOrDefaultAsync(g => g.Code == dto.Code && g.CustomerId == customerId);

        if (card == null) return (false, "Hediye karti bulunamadi");
        if (!card.IsActive) return (false, "Bu hediye karti aktif degil");
        if (card.ExpiresAt.HasValue && card.ExpiresAt.Value < DateTime.UtcNow) return (false, "Bu hediye kartinin suresi dolmus");
        if (card.RemainingBalance < dto.Amount) return (false, $"Yetersiz bakiye. Kalan: {card.RemainingBalance:N2} TL");

        card.RemainingBalance -= dto.Amount;
        if (card.RemainingBalance == 0) card.IsActive = false;

        _transactions.Add(new SlnGiftCardTransaction
        {
            GiftCardId = card.Id,
            TransactionTypeId = 2, // Harcama
            Amount = dto.Amount,
            Description = dto.InvoiceId.HasValue ? $"Adisyon #{dto.InvoiceId}" : "Hediye karti kullanimi",
            RelatedInvoiceId = dto.InvoiceId
        });

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeactivateGiftCardAsync(int id, int customerId)
    {
        var card = await _cards.GetAllQueryable()
            .FirstOrDefaultAsync(g => g.Id == id && g.CustomerId == customerId);

        if (card == null) return (false, "Hediye karti bulunamadi");
        card.IsActive = false;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static string GenerateCode()
    {
        var bytes = new byte[6];
        RandomNumberGenerator.Fill(bytes);
        return "GC-" + Convert.ToHexString(bytes).ToUpper();
    }

    private static SlnGiftCardDto MapToDto(SlnGiftCard g) => new()
    {
        Id = g.Id,
        Code = g.Code,
        OriginalAmount = g.OriginalAmount,
        RemainingBalance = g.RemainingBalance,
        RecipientName = g.RecipientName,
        RecipientPhone = g.RecipientPhone,
        SenderName = g.SenderName,
        Message = g.Message,
        ExpiresAt = g.ExpiresAt,
        IsActive = g.IsActive,
        SoldByName = g.SoldByPersonnel?.User?.FullName,
        CreatedAt = g.CreatedAt,
        Transactions = (g.Transactions ?? []).OrderByDescending(t => t.CreatedAt).Select(t => new SlnGiftCardTransactionDto
        {
            Id = t.Id,
            TransactionTypeId = t.TransactionTypeId,
            Amount = t.Amount,
            Description = t.Description,
            CreatedAt = t.CreatedAt
        }).ToList()
    };
}
