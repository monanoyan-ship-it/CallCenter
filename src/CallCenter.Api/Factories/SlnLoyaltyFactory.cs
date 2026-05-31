using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnLoyaltyFactory : ISlnLoyaltyFactory
{
    private readonly ISlnLoyaltyConfigEntityService _configEs;
    private readonly ISlnClientLoyaltyEntityService _loyaltyEs;
    private readonly ISlnLoyaltyTransactionEntityService _transactionEs;
    private readonly ISlnClientEntityService _clientEs;
    private readonly IUnitOfWork _uow;

    public SlnLoyaltyFactory(
        ISlnLoyaltyConfigEntityService configEs,
        ISlnClientLoyaltyEntityService loyaltyEs,
        ISlnLoyaltyTransactionEntityService transactionEs,
        ISlnClientEntityService clientEs,
        IUnitOfWork uow)
    {
        _configEs = configEs;
        _loyaltyEs = loyaltyEs;
        _transactionEs = transactionEs;
        _clientEs = clientEs;
        _uow = uow;
    }

    public async Task<SlnLoyaltyConfigDto?> GetConfigAsync(int customerId)
    {
        var config = await _configEs.GetAllQueryable().FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (config == null) return null;
        return new SlnLoyaltyConfigDto
        {
            Id = config.Id,
            PointsPerTL = config.PointsPerTL,
            PointValue = config.PointValue,
            MinRedeemPoints = config.MinRedeemPoints,
            IsActive = config.IsActive
        };
    }

    public async Task<SlnLoyaltyConfigDto> SaveConfigAsync(SlnLoyaltyConfigUpdateDto dto, int customerId)
    {
        var config = await _configEs.GetAllQueryable().FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (config == null)
        {
            config = new SlnLoyaltyConfig { CustomerId = customerId };
            _configEs.Add(config);
        }
        config.PointsPerTL = dto.PointsPerTL;
        config.PointValue = dto.PointValue;
        config.MinRedeemPoints = dto.MinRedeemPoints;
        config.IsActive = dto.IsActive;
        await _uow.SaveChangesAsync();

        return (await GetConfigAsync(customerId))!;
    }

    public async Task<List<SlnClientLoyaltyDto>> GetClientLoyaltiesAsync(int customerId, int? branchId = null)
    {
        var config = await _configEs.GetAllQueryable().FirstOrDefaultAsync(c => c.CustomerId == customerId);
        var pointValue = config?.PointValue ?? 0.1m;

        return await SalonBranchScope.ApplyToLoyalties(
                _loyaltyEs.GetAllQueryable().Where(l => l.CustomerId == customerId && l.CurrentBalance > 0),
                branchId)
            .Include(l => l.SlnClient)
            .OrderByDescending(l => l.CurrentBalance)
            .Select(l => new SlnClientLoyaltyDto
            {
                Id = l.Id,
                SlnClientId = l.SlnClientId,
                ClientName = l.SlnClient != null ? l.SlnClient.FullName : "",
                TotalEarned = l.TotalEarned,
                TotalSpent = l.TotalSpent,
                CurrentBalance = l.CurrentBalance,
                BalanceValue = l.CurrentBalance * pointValue
            }).ToListAsync();
    }

    public async Task<SlnClientLoyaltyDto?> GetClientLoyaltyAsync(int slnClientId, int customerId, int? branchId = null)
    {
        var config = await _configEs.GetAllQueryable().FirstOrDefaultAsync(c => c.CustomerId == customerId);
        var pointValue = config?.PointValue ?? 0.1m;

        var clientExists = await SalonBranchScope.ApplyToClients(
                _clientEs.GetAllQueryable().Where(c => c.Id == slnClientId && c.CustomerId == customerId),
                branchId)
            .AnyAsync();
        if (!clientExists) return null;

        var loyalty = await SalonBranchScope.ApplyToLoyalties(
                _loyaltyEs.GetAllQueryable().Where(l => l.SlnClientId == slnClientId && l.CustomerId == customerId),
                branchId)
            .Include(l => l.SlnClient)
            .FirstOrDefaultAsync();

        if (loyalty == null) return new SlnClientLoyaltyDto { SlnClientId = slnClientId };

        return new SlnClientLoyaltyDto
        {
            Id = loyalty.Id,
            SlnClientId = loyalty.SlnClientId,
            ClientName = loyalty.SlnClient?.FullName ?? "",
            TotalEarned = loyalty.TotalEarned,
            TotalSpent = loyalty.TotalSpent,
            CurrentBalance = loyalty.CurrentBalance,
            BalanceValue = loyalty.CurrentBalance * pointValue
        };
    }

    public async Task<List<SlnLoyaltyTransactionDto>> GetTransactionsAsync(int slnClientId, int customerId, int? branchId = null)
    {
        var loyalty = await SalonBranchScope.ApplyToLoyalties(
                _loyaltyEs.GetAllQueryable().Where(l => l.SlnClientId == slnClientId && l.CustomerId == customerId),
                branchId)
            .FirstOrDefaultAsync();

        if (loyalty == null) return [];

        return await _transactionEs.GetAllQueryable()
            .Where(t => t.ClientLoyaltyId == loyalty.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new SlnLoyaltyTransactionDto
            {
                Id = t.Id,
                TransactionTypeId = t.TransactionTypeId,
                Points = t.Points,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            }).ToListAsync();
    }

    public async Task EarnPointsAsync(int slnClientId, decimal invoiceAmount, int? invoiceId, int customerId, int? branchId = null)
    {
        var config = await _configEs.GetAllQueryable().FirstOrDefaultAsync(c => c.CustomerId == customerId && c.IsActive);
        if (config == null) return;

        var clientExists = await SalonBranchScope.ApplyToClients(
                _clientEs.GetAllQueryable().Where(c => c.Id == slnClientId && c.CustomerId == customerId),
                branchId)
            .AnyAsync();
        if (!clientExists) return;

        var points = (int)(invoiceAmount * config.PointsPerTL);
        if (points <= 0) return;

        var loyalty = await GetOrCreateLoyaltyAsync(slnClientId, customerId);
        loyalty.TotalEarned += points;
        loyalty.CurrentBalance += points;

        _transactionEs.Add(new SlnLoyaltyTransaction
        {
            ClientLoyaltyId = loyalty.Id,
            TransactionTypeId = 1,
            Points = points,
            Description = invoiceId.HasValue ? $"Adisyon #{invoiceId} - {invoiceAmount:N0} TL" : $"{invoiceAmount:N0} TL harcama",
            RelatedInvoiceId = invoiceId
        });

        await _uow.SaveChangesAsync();
    }

    public async Task<(bool Success, string? Error)> RedeemPointsAsync(SlnLoyaltyRedeemDto dto, int customerId, int? branchId = null)
    {
        var config = await _configEs.GetAllQueryable().FirstOrDefaultAsync(c => c.CustomerId == customerId && c.IsActive);
        if (config == null) return (false, "Sadakat programi aktif degil");
        if (dto.Points < config.MinRedeemPoints) return (false, $"Minimum {config.MinRedeemPoints} puan gerekli");

        var loyalty = await SalonBranchScope.ApplyToLoyalties(
                _loyaltyEs.GetAllQueryable().Where(l => l.SlnClientId == dto.SlnClientId && l.CustomerId == customerId),
                branchId)
            .FirstOrDefaultAsync();

        if (loyalty == null || loyalty.CurrentBalance < dto.Points)
            return (false, "Yetersiz puan bakiyesi");

        loyalty.TotalSpent += dto.Points;
        loyalty.CurrentBalance -= dto.Points;

        var tlValue = dto.Points * config.PointValue;
        _transactionEs.Add(new SlnLoyaltyTransaction
        {
            ClientLoyaltyId = loyalty.Id,
            TransactionTypeId = 2,
            Points = dto.Points,
            Description = $"{dto.Points} puan kullanildi ({tlValue:N2} TL)",
            RelatedInvoiceId = dto.InvoiceId
        });

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(decimal TLValue, string? Error)> RedeemForInvoiceAsync(int customerId, int slnClientId, int points, int? invoiceId, int? branchId = null)
    {
        if (points <= 0) return (0m, "Puan miktari sifirdan buyuk olmali");
        var config = await _configEs.GetAllQueryable().FirstOrDefaultAsync(c => c.CustomerId == customerId && c.IsActive);
        if (config == null) return (0m, "Sadakat programi aktif degil");
        if (points < config.MinRedeemPoints) return (0m, $"Minimum {config.MinRedeemPoints} puan gerekli");

        var loyalty = await SalonBranchScope.ApplyToLoyalties(
                _loyaltyEs.GetAllQueryable().Where(l => l.SlnClientId == slnClientId && l.CustomerId == customerId),
                branchId)
            .FirstOrDefaultAsync();
        if (loyalty == null || loyalty.CurrentBalance < points)
            return (0m, "Yetersiz puan bakiyesi");

        loyalty.TotalSpent += points;
        loyalty.CurrentBalance -= points;

        var tlValue = Math.Round(points * config.PointValue, 2, MidpointRounding.AwayFromZero);
        _transactionEs.Add(new SlnLoyaltyTransaction
        {
            ClientLoyaltyId = loyalty.Id,
            TransactionTypeId = 2,
            Points = points,
            Description = $"{points} puan adisyon icin kullanildi ({tlValue:N2} TL)",
            RelatedInvoiceId = invoiceId
        });

        await _uow.SaveChangesAsync();
        return (tlValue, null);
    }

    private async Task<SlnClientLoyalty> GetOrCreateLoyaltyAsync(int slnClientId, int customerId)
    {
        var loyalty = await _loyaltyEs.GetAllQueryable()
            .FirstOrDefaultAsync(l => l.SlnClientId == slnClientId && l.CustomerId == customerId);

        if (loyalty == null)
        {
            loyalty = new SlnClientLoyalty
            {
                CustomerId = customerId,
                SlnClientId = slnClientId
            };
            _loyaltyEs.Add(loyalty);
            await _uow.SaveChangesAsync();
        }

        return loyalty;
    }
}
