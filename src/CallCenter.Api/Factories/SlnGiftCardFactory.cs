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
    private readonly ISlnInvoiceEntityService _invoices;
    private readonly ISlnInvoiceItemEntityService _invoiceItems;
    private readonly ISlnCashRegisterEntityService _cashRegisters;
    private readonly ISlnCashTransactionEntityService _cashTransactions;
    private readonly IUnitOfWork _uow;

    public SlnGiftCardFactory(
        ISlnGiftCardEntityService cards,
        ISlnGiftCardTransactionEntityService transactions,
        ISlnInvoiceEntityService invoices,
        ISlnInvoiceItemEntityService invoiceItems,
        ISlnCashRegisterEntityService cashRegisters,
        ISlnCashTransactionEntityService cashTransactions,
        IUnitOfWork uow)
    {
        _cards = cards;
        _transactions = transactions;
        _invoices = invoices;
        _invoiceItems = invoiceItems;
        _cashRegisters = cashRegisters;
        _cashTransactions = cashTransactions;
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
        var normalizedCode = NormalizeCode(code);
        var card = await _cards.GetAllQueryable()
            .Include(g => g.SoldByPersonnel).ThenInclude(p => p!.User)
            .Include(g => g.Transactions)
            .FirstOrDefaultAsync(g => g.Code == normalizedCode && g.CustomerId == customerId && g.IsActive);
        return card != null ? MapToDto(card) : null;
    }

    public async Task<(SlnGiftCardDto? Card, string? Error)> CreateGiftCardAsync(SlnGiftCardCreateDto dto, int userId, int customerId, int? branchId = null)
    {
        if (dto.Amount <= 0) return (null, "Hediye karti tutari 0'dan buyuk olmali");
        if (dto.Amount > 100000) return (null, "Hediye karti tutari guvenlik limiti nedeniyle 100.000 TL'yi asamaz");
        if (dto.ExpiresAt.HasValue && dto.ExpiresAt.Value < DateTime.UtcNow.Date)
            return (null, "Son kullanma tarihi gecmis olamaz");

        var card = new SlnGiftCard
        {
            CustomerId = customerId,
            Code = await GenerateUniqueCodeAsync(customerId),
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

        await CreateGiftCardSaleInvoiceAsync(customerId, branchId, userId, dto.PaymentMethodId, card);

        return ((await GetGiftCardAsync(card.Id, customerId))!, null);
    }

    public async Task<(bool Success, string? Error)> RedeemGiftCardAsync(SlnGiftCardRedeemDto dto, int customerId)
    {
        if (dto.Amount <= 0) return (false, "Hediye karti kullanim tutari 0'dan buyuk olmali");
        var normalizedCode = NormalizeCode(dto.Code);
        var card = await _cards.GetAllQueryable()
            .FirstOrDefaultAsync(g => g.Code == normalizedCode && g.CustomerId == customerId);

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
        if (card.RemainingBalance < card.OriginalAmount)
            return (false, "Kullanilmis hediye karti manuel iptal edilemez. Iade akisindan ilerleyin.");
        card.IsActive = false;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> HasRedemptionForInvoiceAsync(int customerId, int invoiceId)
        => await _transactions.GetAllQueryable()
            .AnyAsync(t => t.RelatedInvoiceId == invoiceId
                && t.TransactionTypeId == 2
                && t.GiftCard != null
                && t.GiftCard.CustomerId == customerId);

    public async Task<(bool Success, string? Error)> ReverseInvoiceRedemptionsAsync(int customerId, int invoiceId)
    {
        var redemptions = await _transactions.GetAllQueryable()
            .Include(t => t.GiftCard)
            .Where(t => t.RelatedInvoiceId == invoiceId
                && t.TransactionTypeId == 2
                && t.GiftCard != null
                && t.GiftCard.CustomerId == customerId)
            .ToListAsync();

        foreach (var tx in redemptions)
        {
            var card = tx.GiftCard!;
            card.RemainingBalance = Math.Min(card.OriginalAmount, card.RemainingBalance + tx.Amount);
            if (card.RemainingBalance > 0 && (!card.ExpiresAt.HasValue || card.ExpiresAt.Value >= DateTime.UtcNow))
                card.IsActive = true;

            _transactions.Add(new SlnGiftCardTransaction
            {
                GiftCardId = card.Id,
                TransactionTypeId = 3,
                Amount = tx.Amount,
                Description = $"Adisyon iade/iptal #{invoiceId}",
                RelatedInvoiceId = invoiceId
            });
        }

        if (redemptions.Count > 0)
            await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CancelGiftCardSaleFromInvoiceAsync(int customerId, string? invoiceNotes)
    {
        var cardId = TryReadNoteInt(invoiceNotes, "GiftCardSale:");
        if (!cardId.HasValue)
            return (true, null);

        var card = await _cards.GetAllQueryable()
            .FirstOrDefaultAsync(g => g.Id == cardId.Value && g.CustomerId == customerId);
        if (card == null)
            return (true, null);

        if (card.RemainingBalance < card.OriginalAmount)
            return (false, "Kullanilmis hediye karti satisi iptal edilemez. Once manuel/pro-rata iade akisi uygulanmali.");

        card.IsActive = false;
        card.RemainingBalance = 0;
        _transactions.Add(new SlnGiftCardTransaction
        {
            GiftCardId = card.Id,
            TransactionTypeId = 3,
            Amount = card.OriginalAmount,
            Description = "Hediye karti satis iptali"
        });
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static string GenerateCode()
    {
        var bytes = new byte[6];
        RandomNumberGenerator.Fill(bytes);
        return "GC-" + Convert.ToHexString(bytes).ToUpper();
    }

    private async Task<string> GenerateUniqueCodeAsync(int customerId)
    {
        for (var i = 0; i < 10; i++)
        {
            var code = GenerateCode();
            var exists = await _cards.GetAllQueryable().AnyAsync(g => g.CustomerId == customerId && g.Code == code);
            if (!exists) return code;
        }

        throw new InvalidOperationException("Hediye karti kodu uretilemedi");
    }

    private async Task CreateGiftCardSaleInvoiceAsync(int customerId, int? branchId, int userId, int paymentMethodId, SlnGiftCard card)
    {
        var today = DateTime.UtcNow;
        var todayCount = await _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId && i.InvoiceDate.Date == today.Date)
            .CountAsync();

        var invoiceNo = $"SLN-{today:yyyyMMdd}-{(todayCount + 1):D4}";
        var invoice = new SlnInvoice
        {
            CustomerId = customerId,
            BranchId = branchId,
            InvoiceNo = invoiceNo,
            InvoiceDate = today,
            TotalAmount = card.OriginalAmount,
            NetAmount = card.OriginalAmount,
            GrandTotal = card.OriginalAmount,
            PaymentMethodId = paymentMethodId > 0 ? paymentMethodId : 1,
            PersonnelId = userId > 0 ? userId : null,
            StatusId = 2,
            Notes = $"GiftCardSale:{card.Id}|GiftCardCode:{card.Code}"
        };

        _invoices.Add(invoice);
        await _uow.SaveChangesAsync();

        _invoiceItems.Add(new SlnInvoiceItem
        {
            InvoiceId = invoice.Id,
            PersonnelId = userId > 0 ? userId : null,
            Quantity = 1,
            UnitPrice = card.OriginalAmount,
            LineTotal = card.OriginalAmount
        });
        await _uow.SaveChangesAsync();

        if (card.OriginalAmount <= 0) return;

        var registerQuery = _cashRegisters.GetAllQueryable()
            .Where(r => r.CustomerId == customerId && r.IsActive);
        var register = branchId.HasValue
            ? await registerQuery.FirstOrDefaultAsync(r => r.BranchId == branchId.Value)
              ?? await registerQuery.FirstOrDefaultAsync(r => r.BranchId == null)
            : await registerQuery.FirstOrDefaultAsync(r => r.BranchId == null)
              ?? await registerQuery.FirstOrDefaultAsync();

        if (register == null) return;

        _cashTransactions.Add(new SlnCashTransaction
        {
            RegisterId = register.Id,
            TransactionTypeId = 1,
            Amount = card.OriginalAmount,
            PaymentMethodId = invoice.PaymentMethodId,
            RelatedInvoiceId = invoice.Id,
            Description = $"Hediye karti satisi: {card.Code} ({invoiceNo})"
        });
        await _uow.SaveChangesAsync();
    }

    private static string NormalizeCode(string code)
        => (code ?? string.Empty).Trim().ToUpperInvariant();

    private static int? TryReadNoteInt(string? notes, string prefix)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return null;

        var index = notes.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        var start = index + prefix.Length;
        var end = start;
        while (end < notes.Length && char.IsDigit(notes[end]))
            end++;

        return end > start && int.TryParse(notes[start..end], out var value) ? value : null;
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
