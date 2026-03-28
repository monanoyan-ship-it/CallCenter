using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnClientFactory
{
    Task<List<SlnClientDto>> GetClientsAsync(int customerId, string? search, int page = 1, int pageSize = 50);
    Task<SlnClientDetailDto?> GetClientDetailAsync(int clientId, int customerId);
    Task<SlnClientDto> CreateClientAsync(SlnClientCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateClientAsync(int clientId, SlnClientUpdateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeleteClientAsync(int clientId, int customerId);
    Task<SlnClientSuggestionsDto> GetSuggestionsAsync(int customerId);
    Task<SlnFormulaDto> AddFormulaAsync(SlnFormulaCreateDto dto, int userId, int customerId);
    Task<(bool Success, string? Error)> DeleteFormulaAsync(int formulaId, int customerId);
}
