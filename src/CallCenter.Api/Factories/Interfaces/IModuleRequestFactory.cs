using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IModuleRequestFactory
{
    Task<List<ModuleRequestDto>> GetCustomerRequestsAsync(int customerId);
    Task<List<ModuleRequestDto>> GetPendingRequestsAsync();
    Task<ModuleRequestDto> CreateRequestAsync(int customerId, int personnelId, CreateModuleRequestDto dto);
    Task<ModuleRequestDto> ApproveRequestAsync(int requestId, int reviewerUserId, string? adminNotes);
    Task<ModuleRequestDto> RejectRequestAsync(int requestId, int reviewerUserId, string? adminNotes);
    Task CancelRequestAsync(int requestId, int customerId);
    Task<List<ModulePricingDto>> GetAvailableModulesAsync(int customerId);
}
