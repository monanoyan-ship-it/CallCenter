using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnPackageFactory : ISlnPackageFactory
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;

    public SlnPackageFactory(AppDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    // ═══ Paket Tanimlari ═══

    public async Task<List<SlnPackageDefinitionDto>> GetDefinitionsAsync(int customerId)
    {
        return await _db.SlnPackageDefinitions
            .Where(d => d.CustomerId == customerId)
            .Include(d => d.Service)
            .OrderBy(d => d.Name)
            .Select(d => new SlnPackageDefinitionDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                ServiceId = d.ServiceId,
                ServiceName = d.Service != null ? d.Service.Name : "",
                TotalSessions = d.TotalSessions,
                Price = d.Price,
                PricePerSession = d.TotalSessions > 0 ? Math.Round(d.Price / d.TotalSessions, 2) : 0,
                ValidDays = d.ValidDays,
                IsActive = d.IsActive
            }).ToListAsync();
    }

    public async Task<SlnPackageDefinitionDto> CreateDefinitionAsync(SlnPackageDefinitionCreateDto dto, int customerId)
    {
        var def = new SlnPackageDefinition
        {
            CustomerId = customerId,
            Name = dto.Name,
            Description = dto.Description,
            ServiceId = dto.ServiceId,
            TotalSessions = dto.TotalSessions,
            Price = dto.Price,
            ValidDays = dto.ValidDays,
            IsActive = dto.IsActive
        };
        _db.SlnPackageDefinitions.Add(def);
        await _uow.SaveChangesAsync();

        return (await GetDefinitionsAsync(customerId)).First(d => d.Id == def.Id);
    }

    public async Task<(bool Success, string? Error)> UpdateDefinitionAsync(int id, SlnPackageDefinitionCreateDto dto, int customerId)
    {
        var def = await _db.SlnPackageDefinitions.FirstOrDefaultAsync(d => d.Id == id && d.CustomerId == customerId);
        if (def == null) return (false, "Paket tanimi bulunamadi");

        def.Name = dto.Name;
        def.Description = dto.Description;
        def.ServiceId = dto.ServiceId;
        def.TotalSessions = dto.TotalSessions;
        def.Price = dto.Price;
        def.ValidDays = dto.ValidDays;
        def.IsActive = dto.IsActive;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteDefinitionAsync(int id, int customerId)
    {
        var def = await _db.SlnPackageDefinitions.FirstOrDefaultAsync(d => d.Id == id && d.CustomerId == customerId);
        if (def == null) return (false, "Paket tanimi bulunamadi");
        _db.SlnPackageDefinitions.Remove(def);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══ Musteri Paketleri ═══

    public async Task<List<SlnClientPackageDto>> GetClientPackagesAsync(int customerId, int? clientId = null)
    {
        var query = _db.SlnClientPackages
            .Where(p => p.CustomerId == customerId)
            .Include(p => p.PackageDefinition).ThenInclude(d => d!.Service)
            .Include(p => p.SlnClient)
            .AsQueryable();

        if (clientId.HasValue)
            query = query.Where(p => p.SlnClientId == clientId.Value);

        return await query.OrderByDescending(p => p.CreatedAt).Select(p => new SlnClientPackageDto
        {
            Id = p.Id,
            PackageName = p.PackageDefinition != null ? p.PackageDefinition.Name : "",
            ServiceName = p.PackageDefinition != null && p.PackageDefinition.Service != null ? p.PackageDefinition.Service.Name : "",
            ClientName = p.SlnClient != null ? p.SlnClient.FullName : null,
            TotalSessions = p.TotalSessions,
            UsedSessions = p.UsedSessions,
            RemainingSessions = p.RemainingSessions,
            PaidAmount = p.PaidAmount,
            ExpiresAt = p.ExpiresAt,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        }).ToListAsync();
    }

    public async Task<(SlnClientPackageDto? Package, string? Error)> SellPackageAsync(SlnClientPackageSellDto dto, int userId, int customerId)
    {
        var def = await _db.SlnPackageDefinitions.FirstOrDefaultAsync(d => d.Id == dto.PackageDefinitionId && d.CustomerId == customerId);
        if (def == null) return (null, "Paket tanimi bulunamadi");

        var pkg = new SlnClientPackage
        {
            CustomerId = customerId,
            PackageDefinitionId = def.Id,
            SlnClientId = dto.SlnClientId,
            TotalSessions = def.TotalSessions,
            UsedSessions = 0,
            RemainingSessions = def.TotalSessions,
            PaidAmount = def.Price,
            ExpiresAt = DateTime.UtcNow.AddDays(def.ValidDays),
            IsActive = true,
            SoldByPersonnelId = userId
        };
        _db.SlnClientPackages.Add(pkg);
        await _uow.SaveChangesAsync();

        var result = (await GetClientPackagesAsync(customerId)).First(p => p.Id == pkg.Id);
        return (result, null);
    }

    public async Task<(bool Success, string? Error)> UseSessionAsync(SlnPackageUseDto dto, int userId, int customerId)
    {
        var pkg = await _db.SlnClientPackages.FirstOrDefaultAsync(p => p.Id == dto.ClientPackageId && p.CustomerId == customerId);
        if (pkg == null) return (false, "Paket bulunamadi");
        if (!pkg.IsActive) return (false, "Bu paket aktif degil");
        if (pkg.RemainingSessions <= 0) return (false, "Kalan seans yok");
        if (pkg.ExpiresAt.HasValue && pkg.ExpiresAt.Value < DateTime.UtcNow) return (false, "Paketin suresi dolmus");

        pkg.UsedSessions++;
        pkg.RemainingSessions--;
        if (pkg.RemainingSessions == 0) pkg.IsActive = false;

        _db.SlnPackageUsages.Add(new SlnPackageUsage
        {
            ClientPackageId = pkg.Id,
            PersonnelId = userId,
            Notes = dto.Notes
        });

        await _uow.SaveChangesAsync();
        return (true, null);
    }
}
