using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnNoShowPolicyFactory : ISlnNoShowPolicyFactory
{
    private readonly ISlnNoShowPolicyEntityService _policies;
    private readonly IUnitOfWork _uow;

    public SlnNoShowPolicyFactory(ISlnNoShowPolicyEntityService policies, IUnitOfWork uow)
    {
        _policies = policies;
        _uow = uow;
    }

    public async Task<SlnNoShowPolicyDto?> GetPolicyAsync(int customerId)
    {
        var policy = await _policies.GetAllQueryable().FirstOrDefaultAsync(p => p.CustomerId == customerId);
        if (policy == null) return null;
        return MapToDto(policy);
    }

    public async Task<SlnNoShowPolicyDto> SavePolicyAsync(SlnNoShowPolicyUpdateDto dto, int customerId)
    {
        var policy = await _policies.GetAllQueryable().FirstOrDefaultAsync(p => p.CustomerId == customerId);
        if (policy == null)
        {
            policy = new SlnNoShowPolicy { CustomerId = customerId };
            _policies.Add(policy);
        }
        policy.RequireDeposit = dto.RequireDeposit;
        policy.DepositAmount = dto.DepositAmount;
        policy.FreeCancellationHours = dto.FreeCancellationHours;
        policy.LateCancellationFee = dto.LateCancellationFee;
        policy.NoShowFee = dto.NoShowFee;
        policy.BlacklistThreshold = dto.BlacklistThreshold;
        policy.IsActive = dto.IsActive;
        await _uow.SaveChangesAsync();

        return (await GetPolicyAsync(customerId))!;
    }

    private static SlnNoShowPolicyDto MapToDto(SlnNoShowPolicy p) => new()
    {
        Id = p.Id,
        RequireDeposit = p.RequireDeposit,
        DepositAmount = p.DepositAmount,
        FreeCancellationHours = p.FreeCancellationHours,
        LateCancellationFee = p.LateCancellationFee,
        NoShowFee = p.NoShowFee,
        BlacklistThreshold = p.BlacklistThreshold,
        IsActive = p.IsActive
    };
}
