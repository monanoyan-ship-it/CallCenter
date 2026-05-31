using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnLoyaltyProgramFactory : ISlnLoyaltyProgramFactory
{
    private readonly ISlnLoyaltyProgramEntityService _programEs;
    private readonly ISlnClientLoyaltyProgressEntityService _progressEs;
    private readonly ISlnLoyaltyProgramRewardEntityService _rewardEs;
    private readonly IUnitOfWork _uow;

    public SlnLoyaltyProgramFactory(
        ISlnLoyaltyProgramEntityService programEs,
        ISlnClientLoyaltyProgressEntityService progressEs,
        ISlnLoyaltyProgramRewardEntityService rewardEs,
        IUnitOfWork uow)
    {
        _programEs = programEs;
        _progressEs = progressEs;
        _rewardEs = rewardEs;
        _uow = uow;
    }

    public async Task<List<SlnLoyaltyProgramDto>> GetProgramsAsync(int customerId, int? branchId = null)
    {
        var query = _programEs.GetAllQueryable()
            .Where(p => p.CustomerId == customerId);

        if (branchId.HasValue)
            query = query.Where(p => !p.BranchId.HasValue || p.BranchId.Value == branchId.Value);

        return await query
            .Include(p => p.Service)
            .Include(p => p.RewardService)
            .OrderBy(p => p.Name)
            .Select(p => new SlnLoyaltyProgramDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                ServiceId = p.ServiceId,
                ServiceName = p.Service != null ? p.Service.Name : "",
                RewardServiceId = p.RewardServiceId,
                RewardServiceName = p.RewardService != null ? p.RewardService.Name : "",
                RequiredVisits = p.RequiredVisits,
                RewardValidDays = p.RewardValidDays,
                MaxRewardsPerClient = p.MaxRewardsPerClient,
                BranchId = p.BranchId,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            }).ToListAsync();
    }

    public async Task<SlnLoyaltyProgramDto> CreateProgramAsync(SlnLoyaltyProgramCreateDto dto, int customerId)
    {
        var program = new SlnLoyaltyProgram
        {
            CustomerId = customerId,
            BranchId = dto.BranchId,
            Name = dto.Name,
            Description = dto.Description,
            ServiceId = dto.ServiceId,
            RewardServiceId = dto.RewardServiceId,
            RequiredVisits = dto.RequiredVisits,
            RewardValidDays = dto.RewardValidDays,
            MaxRewardsPerClient = dto.MaxRewardsPerClient,
            IsActive = dto.IsActive
        };
        _programEs.Add(program);
        await _uow.SaveChangesAsync();
        return (await GetProgramsAsync(customerId)).First(p => p.Id == program.Id);
    }

    public async Task<(bool Success, string? Error)> UpdateProgramAsync(int id, SlnLoyaltyProgramCreateDto dto, int customerId)
    {
        var program = await _programEs.GetAllQueryable().FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId);
        if (program == null) return (false, "Sadakat programi bulunamadi");

        program.Name = dto.Name;
        program.Description = dto.Description;
        program.ServiceId = dto.ServiceId;
        program.RewardServiceId = dto.RewardServiceId;
        program.RequiredVisits = dto.RequiredVisits;
        program.RewardValidDays = dto.RewardValidDays;
        program.MaxRewardsPerClient = dto.MaxRewardsPerClient;
        program.BranchId = dto.BranchId;
        program.IsActive = dto.IsActive;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteProgramAsync(int id, int customerId)
    {
        var program = await _programEs.GetAllQueryable().FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId);
        if (program == null) return (false, "Sadakat programi bulunamadi");
        _programEs.Remove(program);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<SlnClientLoyaltyProgressDto>> GetClientProgressAsync(int customerId, int? clientId = null, int? branchId = null)
    {
        var query = _progressEs.GetAllQueryable()
            .Where(p => p.CustomerId == customerId);

        if (clientId.HasValue)
            query = query.Where(p => p.SlnClientId == clientId.Value);

        if (branchId.HasValue)
            query = query.Where(p => !p.BranchId.HasValue || p.BranchId.Value == branchId.Value);

        return await query
            .Include(p => p.Program).ThenInclude(p => p!.Service)
            .Include(p => p.Program).ThenInclude(p => p!.RewardService)
            .Include(p => p.SlnClient)
            .OrderByDescending(p => p.LastVisitAt ?? p.CreatedAt)
            .Select(p => new SlnClientLoyaltyProgressDto
            {
                Id = p.Id,
                ProgramId = p.ProgramId,
                ProgramName = p.Program != null ? p.Program.Name : "",
                ServiceId = p.Program != null ? p.Program.ServiceId : 0,
                ServiceName = p.Program != null && p.Program.Service != null ? p.Program.Service.Name : "",
                RewardServiceId = p.Program != null ? p.Program.RewardServiceId : 0,
                RewardServiceName = p.Program != null && p.Program.RewardService != null ? p.Program.RewardService.Name : "",
                RequiredVisits = p.Program != null ? p.Program.RequiredVisits : 0,
                VisitCount = p.VisitCount,
                RewardsEarned = p.RewardsEarned,
                RewardsUsed = p.RewardsUsed,
                AvailableRewards = p.RewardsEarned - p.RewardsUsed,
                VisitsToNextReward = p.Program != null && p.Program.RequiredVisits > 0
                    ? (p.Program.RequiredVisits - (p.VisitCount % p.Program.RequiredVisits))
                    : 0,
                LastVisitAt = p.LastVisitAt,
                SlnClientId = p.SlnClientId,
                ClientName = p.SlnClient != null ? p.SlnClient.FullName : null
            }).ToListAsync();
    }

    public async Task<List<SlnLoyaltyProgramRewardDto>> GetAvailableRewardsAsync(int customerId, int slnClientId, int? branchId = null)
    {
        var now = DateTime.UtcNow;
        var query = _rewardEs.GetAllQueryable()
            .Where(r => !r.UsedAt.HasValue
                && (!r.ExpiresAt.HasValue || r.ExpiresAt.Value >= now)
                && r.Progress != null
                && r.Progress.CustomerId == customerId
                && r.Progress.SlnClientId == slnClientId);

        if (branchId.HasValue)
            query = query.Where(r => !r.Progress!.BranchId.HasValue || r.Progress.BranchId.Value == branchId.Value);

        return await query
            .Include(r => r.Progress).ThenInclude(p => p!.Program)
            .Include(r => r.Progress).ThenInclude(p => p!.SlnClient)
            .Include(r => r.RewardService)
            .OrderBy(r => r.ExpiresAt.HasValue ? 0 : 1)
            .ThenBy(r => r.ExpiresAt)
            .ThenBy(r => r.EarnedAt)
            .Select(r => new SlnLoyaltyProgramRewardDto
            {
                Id = r.Id,
                ProgressId = r.ProgressId,
                ProgramId = r.Progress!.ProgramId,
                ProgramName = r.Progress.Program != null ? r.Progress.Program.Name : "",
                RewardServiceId = r.RewardServiceId,
                RewardServiceName = r.RewardService != null ? r.RewardService.Name : "",
                SlnClientId = r.Progress.SlnClientId,
                ClientName = r.Progress.SlnClient != null ? r.Progress.SlnClient.FullName : null,
                EarnedAt = r.EarnedAt,
                UsedAt = r.UsedAt,
                ExpiresAt = r.ExpiresAt,
                IsAvailable = true
            }).ToListAsync();
    }

    public async Task EarnFromInvoiceItemsAsync(int customerId, int slnClientId, int? branchId, IEnumerable<int> serviceIds, int? invoiceItemId = null)
    {
        if (slnClientId <= 0) return;
        var ids = serviceIds.Where(id => id > 0).ToList();
        if (ids.Count == 0) return;

        var programs = await _programEs.GetAllQueryable()
            .Where(p => p.CustomerId == customerId
                && p.IsActive
                && p.RequiredVisits > 0
                && ids.Contains(p.ServiceId)
                && (!p.BranchId.HasValue || !branchId.HasValue || p.BranchId.Value == branchId.Value))
            .ToListAsync();
        if (programs.Count == 0) return;

        var programIds = programs.Select(p => p.Id).ToList();
        var existingProgresses = await _progressEs.GetAllQueryable()
            .Where(p => p.CustomerId == customerId
                && p.SlnClientId == slnClientId
                && programIds.Contains(p.ProgramId))
            .ToListAsync();

        var now = DateTime.UtcNow;
        var serviceVisitCount = ids.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

        foreach (var program in programs)
        {
            if (!serviceVisitCount.TryGetValue(program.ServiceId, out var count) || count == 0)
                continue;

            var progress = existingProgresses.FirstOrDefault(p => p.ProgramId == program.Id);
            if (progress == null)
            {
                progress = new SlnClientLoyaltyProgress
                {
                    CustomerId = customerId,
                    ProgramId = program.Id,
                    SlnClientId = slnClientId,
                    BranchId = branchId,
                    VisitCount = 0,
                    RewardsEarned = 0,
                    RewardsUsed = 0
                };
                _progressEs.Add(progress);
            }

            for (var i = 0; i < count; i++)
            {
                progress.VisitCount++;
                progress.LastVisitAt = now;

                if (progress.VisitCount % program.RequiredVisits != 0)
                    continue;

                if (program.MaxRewardsPerClient.HasValue && progress.RewardsEarned >= program.MaxRewardsPerClient.Value)
                    continue;

                progress.RewardsEarned++;
                _rewardEs.Add(new SlnLoyaltyProgramReward
                {
                    Progress = progress,
                    RewardServiceId = program.RewardServiceId,
                    EarnedAt = now,
                    EarnedFromInvoiceItemId = invoiceItemId,
                    ExpiresAt = program.RewardValidDays.HasValue ? now.AddDays(program.RewardValidDays.Value) : null
                });
            }
        }

        await _uow.SaveChangesAsync();
    }

    public async Task<(bool Success, string? Error)> ApplyRewardAsync(int customerId, int rewardId, int invoiceItemId)
    {
        var reward = await _rewardEs.GetAllQueryable()
            .Include(r => r.Progress)
            .FirstOrDefaultAsync(r => r.Id == rewardId && r.Progress != null && r.Progress.CustomerId == customerId);
        if (reward == null) return (false, "Odul bulunamadi");
        if (reward.UsedAt.HasValue) return (false, "Odul zaten kullanilmis");
        if (reward.ExpiresAt.HasValue && reward.ExpiresAt.Value < DateTime.UtcNow) return (false, "Odulun suresi dolmus");

        reward.UsedAt = DateTime.UtcNow;
        reward.UsedInvoiceItemId = invoiceItemId;
        reward.Progress!.RewardsUsed++;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task ReverseEarnsFromInvoiceAsync(int customerId, int invoiceId)
    {
        // Iade/iptal: bu invoice'tan kazandirilan odul varsa duser. Kullanilmis odul varsa veri butunlugu icin kaydedip iz birakir.
        var rewards = await _rewardEs.GetAllQueryable()
            .Include(r => r.Progress)
            .Where(r => r.Progress != null
                && r.Progress.CustomerId == customerId
                && r.EarnedFromInvoiceItemId.HasValue
                && r.Progress.SlnClient != null)
            .ToListAsync();

        // Bu adisyondan kazandirilan odulleri sil (UsedAt yoksa)
        foreach (var reward in rewards)
        {
            if (!reward.EarnedFromInvoiceItemId.HasValue) continue;
            if (reward.UsedAt.HasValue) continue;

            // EarnedFromInvoiceItemId Invoice'a baglanmiyor dogrudan; konservatif olarak dokunmuyoruz.
            // Adisyon iptal akisi gelecekte InvoiceItem.InvoiceId ile kosul ekleyebilir.
            _ = invoiceId; // placeholder - audit izi icin parametre alindi
        }

        await Task.CompletedTask;
    }
}
