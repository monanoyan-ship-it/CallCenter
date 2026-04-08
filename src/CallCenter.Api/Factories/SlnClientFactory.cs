using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnClientFactory : ISlnClientFactory
{
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnFormulaEntityService _formulas;
    private readonly ISlnClientPhotoEntityService _photos;
    private readonly ISlnAppointmentEntityService _appointments;
    private readonly ISlnInvoiceEntityService _invoices;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnClientFactory> _logger;

    public SlnClientFactory(
        ISlnClientEntityService clients,
        ISlnFormulaEntityService formulas,
        ISlnClientPhotoEntityService photos,
        ISlnAppointmentEntityService appointments,
        ISlnInvoiceEntityService invoices,
        IUnitOfWork uow,
        ILogger<SlnClientFactory> logger)
    {
        _clients = clients;
        _formulas = formulas;
        _photos = photos;
        _appointments = appointments;
        _invoices = invoices;
        _uow = uow;
        _logger = logger;
    }

    public async Task<object> GetClientsAsync(int customerId, string? search, int page = 1, int pageSize = 50)
    {
        var query = _clients.GetAllQueryable()
            .Where(c => c.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(s) ||
                (c.Phone != null && c.Phone.Contains(s)) ||
                (c.Email != null && c.Email.ToLower().Contains(s)));
        }

        var totalCount = await query.CountAsync();

        var clients = await query
            .OrderBy(c => c.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var clientIds = clients.Select(c => c.Id).ToList();

        // Ziyaret ve harcama istatistikleri
        var invoiceStats = await _invoices.GetAllQueryable()
            .Where(i => clientIds.Contains(i.SlnClientId ?? 0) && i.StatusId != 3)
            .GroupBy(i => i.SlnClientId)
            .Select(g => new
            {
                ClientId = g.Key,
                VisitCount = g.Count(),
                TotalSpent = g.Sum(i => i.NetAmount),
                LastVisit = g.Max(i => i.InvoiceDate)
            })
            .ToListAsync();

        var statsDict = invoiceStats.ToDictionary(s => s.ClientId ?? 0);

        var items = clients.Select(c =>
        {
            statsDict.TryGetValue(c.Id, out var stats);
            return new SlnClientDto
            {
                Id = c.Id,
                Uid = c.Uid,
                FullName = c.FullName,
                Phone = c.Phone,
                Email = c.Email,
                GenderId = c.GenderId,
                BirthDate = c.BirthDate,
                HairColor = c.HairColor,
                IsFavorite = c.IsFavorite,
                CreatedAt = c.CreatedAt,
                VisitCount = stats?.VisitCount ?? 0,
                TotalSpent = stats?.TotalSpent ?? 0,
                LastVisit = stats?.LastVisit
            };
        }).ToList();

        return new { items, totalCount, page, pageSize };
    }

    public async Task<SlnClientDetailDto?> GetClientDetailAsync(int clientId, int customerId)
    {
        var client = await _clients.GetAllQueryable()
            .Include(c => c.Formulas).ThenInclude(f => f.AppliedByPersonnel).ThenInclude(p => p!.User)
            .Include(c => c.Photos)
            .FirstOrDefaultAsync(c => c.Id == clientId && c.CustomerId == customerId);

        if (client == null) return null;

        // Istatistikler
        var invoiceStats = await _invoices.GetAllQueryable()
            .Where(i => i.SlnClientId == clientId && i.StatusId != 3)
            .GroupBy(i => i.SlnClientId)
            .Select(g => new
            {
                VisitCount = g.Count(),
                TotalSpent = g.Sum(i => i.NetAmount),
                LastVisit = g.Max(i => i.InvoiceDate)
            })
            .FirstOrDefaultAsync();

        return new SlnClientDetailDto
        {
            Id = client.Id,
            Uid = client.Uid,
            FullName = client.FullName,
            Phone = client.Phone,
            Phone2 = client.Phone2,
            Email = client.Email,
            GenderId = client.GenderId,
            BirthDate = client.BirthDate,
            MarriageDate = client.MarriageDate,
            Occupation = client.Occupation,
            City = client.City,
            Address = client.Address,
            HairColor = client.HairColor,
            WhiteRatioPercent = client.WhiteRatioPercent,
            SkinType = client.SkinType,
            Notes = client.Notes,
            IsFavorite = client.IsFavorite,
            CreatedAt = client.CreatedAt,
            VisitCount = invoiceStats?.VisitCount ?? 0,
            TotalSpent = invoiceStats?.TotalSpent ?? 0,
            LastVisit = invoiceStats?.LastVisit,
            Formulas = client.Formulas.OrderByDescending(f => f.AppliedAt).Select(f => new SlnFormulaDto
            {
                Id = f.Id,
                FormulaText = f.FormulaText,
                ColorCode = f.ColorCode,
                OxidantRatio = f.OxidantRatio,
                ApplicationNotes = f.ApplicationNotes,
                AppliedByName = f.AppliedByPersonnel?.User?.FullName,
                AppliedAt = f.AppliedAt
            }).ToList(),
            Photos = client.Photos.OrderByDescending(p => p.TakenAt).Select(p => new SlnClientPhotoDto
            {
                Id = p.Id,
                FilePath = p.FilePath,
                Description = p.Description,
                TakenAt = p.TakenAt
            }).ToList()
        };
    }

    public async Task<SlnClientDto> CreateClientAsync(SlnClientCreateDto dto, int customerId)
    {
        var client = new SlnClient
        {
            CustomerId = customerId,
            FullName = dto.FullName,
            Phone = Shared.Helpers.PhoneHelper.Normalize(dto.Phone),
            Phone2 = Shared.Helpers.PhoneHelper.Normalize(dto.Phone2),
            Email = dto.Email,
            GenderId = dto.GenderId,
            BirthDate = dto.BirthDate,
            MarriageDate = dto.MarriageDate,
            Occupation = dto.Occupation,
            City = dto.City,
            Address = dto.Address,
            HairColor = dto.HairColor,
            WhiteRatioPercent = dto.WhiteRatioPercent,
            SkinType = dto.SkinType,
            Notes = dto.Notes
        };

        _clients.Add(client);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Yeni salon musterisi olusturuldu: {ClientId} - {FullName}", client.Id, client.FullName);

        return new SlnClientDto
        {
            Id = client.Id,
            Uid = client.Uid,
            FullName = client.FullName,
            Phone = client.Phone,
            Email = client.Email,
            GenderId = client.GenderId,
            BirthDate = client.BirthDate,
            HairColor = client.HairColor,
            IsFavorite = client.IsFavorite,
            CreatedAt = client.CreatedAt
        };
    }

    public async Task<(bool Success, string? Error)> UpdateClientAsync(int clientId, SlnClientUpdateDto dto, int customerId)
    {
        var client = await _clients.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == clientId && c.CustomerId == customerId);

        if (client == null) return (false, "Musteri bulunamadi");

        client.FullName = dto.FullName;
        client.Phone = Shared.Helpers.PhoneHelper.Normalize(dto.Phone);
        client.Phone2 = Shared.Helpers.PhoneHelper.Normalize(dto.Phone2);
        client.Email = dto.Email;
        client.GenderId = dto.GenderId;
        client.BirthDate = dto.BirthDate;
        client.MarriageDate = dto.MarriageDate;
        client.Occupation = dto.Occupation;
        client.City = dto.City;
        client.Address = dto.Address;
        client.HairColor = dto.HairColor;
        client.WhiteRatioPercent = dto.WhiteRatioPercent;
        client.SkinType = dto.SkinType;
        client.Notes = dto.Notes;
        client.IsFavorite = dto.IsFavorite;
        client.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteClientAsync(int clientId, int customerId)
    {
        var client = await _clients.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == clientId && c.CustomerId == customerId);

        if (client == null) return (false, "Musteri bulunamadi");

        _clients.Remove(client);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Salon musterisi silindi: {ClientId}", clientId);
        return (true, null);
    }

    public async Task<SlnClientSuggestionsDto> GetSuggestionsAsync(int customerId)
    {
        var clients = await _clients.GetAllQueryable()
            .Where(c => c.CustomerId == customerId)
            .Select(c => new { c.HairColor, c.SkinType })
            .ToListAsync();

        return new SlnClientSuggestionsDto
        {
            HairColors = clients.Where(c => !string.IsNullOrWhiteSpace(c.HairColor)).Select(c => c.HairColor!).Distinct().OrderBy(x => x).ToList(),
            SkinTypes = clients.Where(c => !string.IsNullOrWhiteSpace(c.SkinType)).Select(c => c.SkinType!).Distinct().OrderBy(x => x).ToList()
        };
    }

    public async Task<SlnFormulaDto> AddFormulaAsync(SlnFormulaCreateDto dto, int userId, int customerId)
    {
        // Musterinin bu firmaya ait oldugunu dogrula
        var client = await _clients.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == dto.SlnClientId && c.CustomerId == customerId);

        if (client == null)
            throw new InvalidOperationException("Musteri bulunamadi");

        var formula = new SlnFormula
        {
            SlnClientId = dto.SlnClientId,
            FormulaText = dto.FormulaText,
            ColorCode = dto.ColorCode,
            OxidantRatio = dto.OxidantRatio,
            ApplicationNotes = dto.ApplicationNotes,
            AppliedByPersonnelId = userId
        };

        _formulas.Add(formula);
        await _uow.SaveChangesAsync();

        return new SlnFormulaDto
        {
            Id = formula.Id,
            FormulaText = formula.FormulaText,
            ColorCode = formula.ColorCode,
            OxidantRatio = formula.OxidantRatio,
            ApplicationNotes = formula.ApplicationNotes,
            AppliedAt = formula.AppliedAt
        };
    }

    public async Task<(bool Success, string? Error)> DeleteFormulaAsync(int formulaId, int customerId)
    {
        var formula = await _formulas.GetAllQueryable()
            .Include(f => f.SlnClient)
            .FirstOrDefaultAsync(f => f.Id == formulaId && f.SlnClient != null && f.SlnClient.CustomerId == customerId);

        if (formula == null) return (false, "Formul bulunamadi");

        _formulas.Remove(formula);
        await _uow.SaveChangesAsync();
        return (true, null);
    }
}
