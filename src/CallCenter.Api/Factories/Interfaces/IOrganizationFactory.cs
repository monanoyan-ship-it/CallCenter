using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IOrganizationFactory
{
    Task<List<OrgUnitTreeDto>> GetTreeAsync(int customerId);
    Task<List<OrgUnitListDto>> GetListAsync(int customerId);
    Task<OrgUnitDetailDto?> GetByIdAsync(int customerId, int id);
    Task<(bool Success, object Result)> CreateAsync(int customerId, OrgUnitCreateDto dto);
    Task<(bool Success, string? Error)> UpdateAsync(int customerId, int id, OrgUnitUpdateDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(int customerId, int id);
    Task<List<OrgUnitListDto>> GetPotentialParentsAsync(int customerId, int? excludeId);
}
