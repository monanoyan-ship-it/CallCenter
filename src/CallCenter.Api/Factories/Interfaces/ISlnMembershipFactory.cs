using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnMembershipFactory
{
    Task<List<SlnMembershipPlanDto>> GetPlansAsync(int customerId, int? branchId = null);
    Task<SlnMembershipPlanDto> CreatePlanAsync(SlnMembershipPlanCreateDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> UpdatePlanAsync(int id, SlnMembershipPlanCreateDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> DeletePlanAsync(int id, int customerId, int? branchId = null);
    Task<List<SlnClientMembershipDto>> GetMembershipsAsync(int customerId, int? clientId = null, int? branchId = null);
    Task<(SlnClientMembershipDto? Membership, string? Error)> CreateMembershipAsync(SlnClientMembershipCreateDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> CancelMembershipAsync(int id, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> FreezeMembershipAsync(int id, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> ReactivateMembershipAsync(int id, int customerId, int? branchId = null);

    /// <summary>Musteri icin hizmet bazli uyelik hak kontrolu. Ucretsiz hak varsa kalan adet doner.</summary>
    Task<List<ServiceMembershipBenefit>> CheckBenefitsAsync(int customerId, int slnClientId, List<int> serviceIds, int? branchId = null);

    /// <summary>Ucretsiz hizmet kullanildiktan sonra sayaci artir</summary>
    Task RecordUsageAsync(int customerId, int membershipId, int serviceId);
}

/// <summary>Bir hizmet icin uyelik avantaji bilgisi</summary>
public class ServiceMembershipBenefit
{
    public int? MembershipId { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public bool HasFreeBenefit { get; set; }
    public int FreeCount { get; set; }
    public int UsedThisPeriod { get; set; }
    public int RemainingFree => Math.Max(0, FreeCount - UsedThisPeriod);
    public int? DiscountPercent { get; set; }
    public string? PlanName { get; set; }
}
