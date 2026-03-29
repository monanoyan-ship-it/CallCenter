using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnPersonnelPriceFactory
{
    Task<List<SlnPersonnelServicePriceDto>> GetPricesAsync(int customerId);
    Task<SlnPersonnelServicePriceDto> CreateOrUpdatePriceAsync(SlnPersonnelServicePriceCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeletePriceAsync(int id, int customerId);
    Task<List<SlnRevenueShareDto>> GetRevenueSharesAsync(int customerId);
    Task<SlnRevenueShareDto> SaveRevenueShareAsync(SlnRevenueShareCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeleteRevenueShareAsync(int id, int customerId);
}
