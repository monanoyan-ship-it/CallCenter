using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnBranchFactory : ISlnBranchFactory
{
    private readonly ISlnBranchEntityService _branches;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnBranchFactory> _logger;

    public SlnBranchFactory(
        ISlnBranchEntityService branches,
        ICustomerPersonnelEntityService personnel,
        IUnitOfWork uow,
        ILogger<SlnBranchFactory> logger)
    {
        _branches = branches;
        _personnel = personnel;
        _uow = uow;
        _logger = logger;
    }

    public async Task<List<SlnBranchDto>> GetBranchesAsync(int customerId)
    {
        var branches = await _branches.GetAllQueryable()
            .Where(b => b.CustomerId == customerId)
            .OrderBy(b => b.Name)
            .ToListAsync();

        // Manager isimlerini cek
        var managerIds = branches
            .Where(b => b.ManagerPersonnelId.HasValue)
            .Select(b => b.ManagerPersonnelId!.Value)
            .Distinct()
            .ToList();

        var managerNames = new Dictionary<int, string>();
        if (managerIds.Count > 0)
        {
            managerNames = await _personnel.GetAllQueryable()
                .Where(p => managerIds.Contains(p.Id))
                .Include(p => p.User)
                .ToDictionaryAsync(p => p.Id, p => p.User?.FullName ?? "");
        }

        return branches.Select(b => MapToDto(b, b.ManagerPersonnelId.HasValue
            ? managerNames.GetValueOrDefault(b.ManagerPersonnelId.Value) : null)).ToList();
    }

    public async Task<SlnBranchDto?> GetBranchAsync(int branchId, int customerId)
    {
        var branch = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.Id == branchId && b.CustomerId == customerId);

        if (branch == null) return null;

        string? managerName = null;
        if (branch.ManagerPersonnelId.HasValue)
        {
            var manager = await _personnel.GetAllQueryable()
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == branch.ManagerPersonnelId.Value);
            managerName = manager?.User?.FullName;
        }

        return MapToDto(branch, managerName);
    }

    public async Task<SlnBranchDto> CreateBranchAsync(SlnBranchCreateDto dto, int customerId)
    {
        var branch = new SlnBranch
        {
            CustomerId = customerId,
            Name = dto.Name,
            Slug = dto.Slug,
            Address = dto.Address,
            City = dto.City,
            District = dto.District,
            Phone = dto.Phone,
            Email = dto.Email,
            GoogleMapsUrl = dto.GoogleMapsUrl,
            WorkingHoursJson = dto.WorkingHoursJson,
            ManagerPersonnelId = dto.ManagerPersonnelId,
            IsHeadquarter = dto.IsHeadquarter,
            IsActive = dto.IsActive,
            CompanyTitle = dto.CompanyTitle,
            TaxOffice = dto.TaxOffice,
            TaxNumber = dto.TaxNumber,
            MersisNo = dto.MersisNo
        };

        _branches.Add(branch);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Yeni sube olusturuldu: {BranchId} - {Name}", branch.Id, branch.Name);
        return MapToDto(branch, null);
    }

    public async Task<(bool Success, string? Error)> UpdateBranchAsync(int branchId, SlnBranchUpdateDto dto, int customerId)
    {
        var branch = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.Id == branchId && b.CustomerId == customerId);

        if (branch == null) return (false, "Sube bulunamadi");

        branch.Name = dto.Name;
        branch.Slug = dto.Slug;
        branch.Address = dto.Address;
        branch.City = dto.City;
        branch.District = dto.District;
        branch.Phone = dto.Phone;
        branch.Email = dto.Email;
        branch.GoogleMapsUrl = dto.GoogleMapsUrl;
        branch.WorkingHoursJson = dto.WorkingHoursJson;
        branch.ManagerPersonnelId = dto.ManagerPersonnelId;
        branch.IsHeadquarter = dto.IsHeadquarter;
        branch.IsActive = dto.IsActive;
        branch.CompanyTitle = dto.CompanyTitle;
        branch.TaxOffice = dto.TaxOffice;
        branch.TaxNumber = dto.TaxNumber;
        branch.MersisNo = dto.MersisNo;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static SlnBranchDto MapToDto(SlnBranch b, string? managerName) => new()
    {
        Id = b.Id,
        Name = b.Name,
        Slug = b.Slug,
        Address = b.Address,
        City = b.City,
        District = b.District,
        Phone = b.Phone,
        Email = b.Email,
        GoogleMapsUrl = b.GoogleMapsUrl,
        WorkingHoursJson = b.WorkingHoursJson,
        PhotoUrl = b.PhotoUrl,
        Latitude = b.Latitude,
        Longitude = b.Longitude,
        ManagerPersonnelId = b.ManagerPersonnelId,
        ManagerName = managerName,
        IsHeadquarter = b.IsHeadquarter,
        IsActive = b.IsActive,
        CompanyTitle = b.CompanyTitle,
        TaxOffice = b.TaxOffice,
        TaxNumber = b.TaxNumber,
        MersisNo = b.MersisNo,
        CreatedAt = b.CreatedAt
    };

    public async Task<(bool Success, string? Error)> DeleteBranchAsync(int branchId, int customerId)
    {
        var branch = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.Id == branchId && b.CustomerId == customerId);

        if (branch == null) return (false, "Sube bulunamadi");

        _branches.Remove(branch);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Sube silindi: {BranchId}", branchId);
        return (true, null);
    }
}
