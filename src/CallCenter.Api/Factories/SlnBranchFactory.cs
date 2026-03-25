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

        return branches.Select(b => new SlnBranchDto
        {
            Id = b.Id,
            Name = b.Name,
            Address = b.Address,
            Phone = b.Phone,
            ManagerPersonnelId = b.ManagerPersonnelId,
            ManagerName = b.ManagerPersonnelId.HasValue
                ? managerNames.GetValueOrDefault(b.ManagerPersonnelId.Value)
                : null,
            IsActive = b.IsActive,
            CreatedAt = b.CreatedAt
        }).ToList();
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

        return new SlnBranchDto
        {
            Id = branch.Id,
            Name = branch.Name,
            Address = branch.Address,
            Phone = branch.Phone,
            ManagerPersonnelId = branch.ManagerPersonnelId,
            ManagerName = managerName,
            IsActive = branch.IsActive,
            CreatedAt = branch.CreatedAt
        };
    }

    public async Task<SlnBranchDto> CreateBranchAsync(SlnBranchCreateDto dto, int customerId)
    {
        var branch = new SlnBranch
        {
            CustomerId = customerId,
            Name = dto.Name,
            Address = dto.Address,
            Phone = dto.Phone,
            ManagerPersonnelId = dto.ManagerPersonnelId,
            IsActive = dto.IsActive
        };

        _branches.Add(branch);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Yeni sube olusturuldu: {BranchId} - {Name}", branch.Id, branch.Name);

        return new SlnBranchDto
        {
            Id = branch.Id,
            Name = branch.Name,
            Address = branch.Address,
            Phone = branch.Phone,
            ManagerPersonnelId = branch.ManagerPersonnelId,
            IsActive = branch.IsActive,
            CreatedAt = branch.CreatedAt
        };
    }

    public async Task<(bool Success, string? Error)> UpdateBranchAsync(int branchId, SlnBranchUpdateDto dto, int customerId)
    {
        var branch = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.Id == branchId && b.CustomerId == customerId);

        if (branch == null) return (false, "Sube bulunamadi");

        branch.Name = dto.Name;
        branch.Address = dto.Address;
        branch.Phone = dto.Phone;
        branch.ManagerPersonnelId = dto.ManagerPersonnelId;
        branch.IsActive = dto.IsActive;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

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
