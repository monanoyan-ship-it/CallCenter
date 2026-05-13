using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnClientFactory
{
    Task<object> GetClientsAsync(int customerId, string? search, int page = 1, int pageSize = 50);
    Task<SlnClientDetailDto?> GetClientDetailAsync(int clientId, int customerId);
    Task<SlnClientDto> CreateClientAsync(SlnClientCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateClientAsync(int clientId, SlnClientUpdateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeleteClientAsync(int clientId, int customerId);
    Task<(bool Success, string? Error)> UpdateHealthInfoAsync(int clientId, SlnClientHealthUpdateDto dto, int customerId, bool requiresReview, int? reviewedByPersonnelId = null);
    Task<(bool Success, string? Error)> ReviewHealthInfoAsync(int clientId, int customerId, int reviewedByPersonnelId);
    Task<SlnClientSuggestionsDto> GetSuggestionsAsync(int customerId);
    Task<SlnFormulaDto> AddFormulaAsync(SlnFormulaCreateDto dto, int userId, int customerId);
    Task<(bool Success, string? Error)> DeleteFormulaAsync(int formulaId, int customerId);
    Task<SlnTreatmentRecordDto> AddTreatmentRecordAsync(SlnTreatmentRecordCreateDto dto, int userId, int customerId);
    Task<(bool Success, string? Error)> DeleteTreatmentRecordAsync(int recordId, int customerId);
    Task<(bool Success, string? Error)> UnblockClientAsync(int clientId, int customerId);
}
