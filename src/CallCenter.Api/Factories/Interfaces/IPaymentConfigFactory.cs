using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IPaymentConfigFactory
{
    Task<List<PaymentConfigListDto>> GetAllAsync();
    Task<PaymentConfigDetailDto?> GetByIdAsync(int id);
    Task<PaymentConfigDetailDto?> GetActiveAsync();
    Task<PaymentBankInfoDto?> GetBankInfoAsync();
    Task<(bool Success, int? Id, string? Error)> CreateAsync(PaymentConfigSaveDto dto);
    Task<(bool Success, string? Error)> UpdateAsync(int id, PaymentConfigSaveDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
    Task<PaymentConfigTestResultDto> TestConnectionAsync(int id, CancellationToken ct = default);
    List<PaymentProviderInfoDto> GetAvailableProviders();
}
