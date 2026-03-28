using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnMembershipFactory
{
    Task<List<SlnMembershipPlanDto>> GetPlansAsync(int customerId);
    Task<SlnMembershipPlanDto> CreatePlanAsync(SlnMembershipPlanCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdatePlanAsync(int id, SlnMembershipPlanCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeletePlanAsync(int id, int customerId);
    Task<List<SlnClientMembershipDto>> GetMembershipsAsync(int customerId, int? clientId = null);
    Task<(SlnClientMembershipDto? Membership, string? Error)> CreateMembershipAsync(SlnClientMembershipCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> CancelMembershipAsync(int id, int customerId);
    Task<(bool Success, string? Error)> FreezeMembershipAsync(int id, int customerId);
    Task<(bool Success, string? Error)> ReactivateMembershipAsync(int id, int customerId);
}
