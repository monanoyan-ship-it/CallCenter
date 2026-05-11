using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISipAccountFactory
{
    // ─── Operator hat tahsisi ───
    Task<SipConnectionInfoDto?> GetMyConnectionAsync(
        int customerId,
        int? personnelId,
        string displayName,
        int? gatewayId = null,
        int? excludeLineId = null,
        bool forceNewLine = false);
    Task<List<SipGatewaySummaryDto>> GetMyGatewaysAsync(int customerId);
    Task ReleaseLineAsync(int personnelId);

    // ─── Admin: Gateway CRUD ───
    Task<PagedResult<SipAccountListDto>> GetAllAsync(int page, int pageSize, int? customerId);
    Task<object?> GetByIdAsync(int id);
    Task<(bool Success, int? Id, string? Error)> CreateAsync(SipAccountCreateDto dto);
    Task<(bool Success, string? Error)> UpdateAsync(int id, SipAccountUpdateDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(int id);

    // ─── Admin: Line CRUD ───
    Task<List<SipLineListDto>> GetLinesByGatewayAsync(int gatewayId);
    Task<(bool Success, int? Id, string? Error)> CreateLineAsync(int gatewayId, SipLineCreateDto dto);
    Task<(bool Success, string? Error)> UpdateLineAsync(int lineId, SipLineUpdateDto dto);
    Task<(bool Success, string? Error)> DeleteLineAsync(int lineId);
}
