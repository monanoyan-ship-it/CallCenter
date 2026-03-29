using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnPersonnelPriceFactory : ISlnPersonnelPriceFactory
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;

    public SlnPersonnelPriceFactory(AppDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    // ═══ Personel Bazli Fiyatlandirma ═══

    public async Task<List<SlnPersonnelServicePriceDto>> GetPricesAsync(int customerId)
    {
        return await _db.SlnPersonnelServicePrices
            .Where(p => p.CustomerId == customerId)
            .Include(p => p.Personnel).ThenInclude(pr => pr!.User)
            .Include(p => p.Service)
            .OrderBy(p => p.Personnel!.User!.FullName)
            .ThenBy(p => p.Service!.Name)
            .Select(p => new SlnPersonnelServicePriceDto
            {
                Id = p.Id,
                PersonnelId = p.PersonnelId,
                PersonnelName = p.Personnel != null && p.Personnel.User != null ? p.Personnel.User.FullName : "",
                ServiceId = p.ServiceId,
                ServiceName = p.Service != null ? p.Service.Name : "",
                Price = p.Price
            })
            .ToListAsync();
    }

    public async Task<SlnPersonnelServicePriceDto> CreateOrUpdatePriceAsync(SlnPersonnelServicePriceCreateDto dto, int customerId)
    {
        var existing = await _db.SlnPersonnelServicePrices
            .FirstOrDefaultAsync(p => p.CustomerId == customerId
                && p.PersonnelId == dto.PersonnelId
                && p.ServiceId == dto.ServiceId);

        if (existing != null)
        {
            existing.Price = dto.Price;
        }
        else
        {
            existing = new SlnPersonnelServicePrice
            {
                CustomerId = customerId,
                PersonnelId = dto.PersonnelId,
                ServiceId = dto.ServiceId,
                Price = dto.Price
            };
            _db.SlnPersonnelServicePrices.Add(existing);
        }
        await _uow.SaveChangesAsync();

        var result = await _db.SlnPersonnelServicePrices
            .Include(p => p.Personnel).ThenInclude(pr => pr!.User)
            .Include(p => p.Service)
            .FirstAsync(p => p.Id == existing.Id);

        return new SlnPersonnelServicePriceDto
        {
            Id = result.Id,
            PersonnelId = result.PersonnelId,
            PersonnelName = result.Personnel?.User?.FullName ?? "",
            ServiceId = result.ServiceId,
            ServiceName = result.Service?.Name ?? "",
            Price = result.Price
        };
    }

    public async Task<(bool Success, string? Error)> DeletePriceAsync(int id, int customerId)
    {
        var price = await _db.SlnPersonnelServicePrices
            .FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId);
        if (price == null) return (false, "Fiyat kaydı bulunamadi");

        _db.SlnPersonnelServicePrices.Remove(price);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══ Hasilat Paylasimi ═══

    public async Task<List<SlnRevenueShareDto>> GetRevenueSharesAsync(int customerId)
    {
        return await _db.SlnRevenueShares
            .Where(r => r.CustomerId == customerId)
            .Include(r => r.Personnel).ThenInclude(pr => pr!.User)
            .OrderBy(r => r.Personnel!.User!.FullName)
            .Select(r => new SlnRevenueShareDto
            {
                Id = r.Id,
                PersonnelId = r.PersonnelId,
                PersonnelName = r.Personnel != null && r.Personnel.User != null ? r.Personnel.User.FullName : "",
                ModelTypeId = r.ModelTypeId,
                PersonnelSharePercent = r.PersonnelSharePercent,
                MonthlyRent = r.MonthlyRent,
                MinimumGuarantee = r.MinimumGuarantee,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<SlnRevenueShareDto> SaveRevenueShareAsync(SlnRevenueShareCreateDto dto, int customerId)
    {
        var existing = await _db.SlnRevenueShares
            .FirstOrDefaultAsync(r => r.CustomerId == customerId && r.PersonnelId == dto.PersonnelId);

        if (existing != null)
        {
            existing.ModelTypeId = dto.ModelTypeId;
            existing.PersonnelSharePercent = dto.PersonnelSharePercent;
            existing.MonthlyRent = dto.MonthlyRent;
            existing.MinimumGuarantee = dto.MinimumGuarantee;
            existing.IsActive = dto.IsActive;
        }
        else
        {
            existing = new SlnRevenueShare
            {
                CustomerId = customerId,
                PersonnelId = dto.PersonnelId,
                ModelTypeId = dto.ModelTypeId,
                PersonnelSharePercent = dto.PersonnelSharePercent,
                MonthlyRent = dto.MonthlyRent,
                MinimumGuarantee = dto.MinimumGuarantee,
                IsActive = dto.IsActive
            };
            _db.SlnRevenueShares.Add(existing);
        }
        await _uow.SaveChangesAsync();

        var result = await _db.SlnRevenueShares
            .Include(r => r.Personnel).ThenInclude(pr => pr!.User)
            .FirstAsync(r => r.Id == existing.Id);

        return new SlnRevenueShareDto
        {
            Id = result.Id,
            PersonnelId = result.PersonnelId,
            PersonnelName = result.Personnel?.User?.FullName ?? "",
            ModelTypeId = result.ModelTypeId,
            PersonnelSharePercent = result.PersonnelSharePercent,
            MonthlyRent = result.MonthlyRent,
            MinimumGuarantee = result.MinimumGuarantee,
            IsActive = result.IsActive,
            CreatedAt = result.CreatedAt
        };
    }

    public async Task<(bool Success, string? Error)> DeleteRevenueShareAsync(int id, int customerId)
    {
        var share = await _db.SlnRevenueShares
            .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);
        if (share == null) return (false, "Hasilat paylasimi bulunamadi");

        _db.SlnRevenueShares.Remove(share);
        await _uow.SaveChangesAsync();
        return (true, null);
    }
}
