using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnNoShowPolicyFactory
{
    Task<SlnNoShowPolicyDto?> GetPolicyAsync(int customerId);
    Task<SlnNoShowPolicyDto> SavePolicyAsync(SlnNoShowPolicyUpdateDto dto, int customerId);
}
