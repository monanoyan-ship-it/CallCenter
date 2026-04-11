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

    /// <summary>Musteri icin hizmet bazli uyelik hak kontrolu. Ucretsiz hak varsa kalan adet doner.</summary>
    Task<List<ServiceMembershipBenefit>> CheckBenefitsAsync(int customerId, int slnClientId, List<int> serviceIds);

    /// <summary>Ucretsiz hizmet kullanildiktan sonra sayaci artir</summary>
    Task RecordUsageAsync(int customerId, int membershipId, int serviceId);
}

/// <summary>Bir hizmet icin uyelik avantaji bilgisi</summary>
public class ServiceMembershipBenefit
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public bool HasFreeBenefit { get; set; }
    public int FreeCountPerMonth { get; set; }
    public int UsedThisMonth { get; set; }
    public int RemainingFree => Math.Max(0, FreeCountPerMonth - UsedThisMonth);
    public int? DiscountPercent { get; set; }
    public string? PlanName { get; set; }
}
