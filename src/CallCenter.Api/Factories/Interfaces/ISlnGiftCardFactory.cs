using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnGiftCardFactory
{
    Task<List<SlnGiftCardDto>> GetGiftCardsAsync(int customerId);
    Task<SlnGiftCardDto?> GetGiftCardAsync(int id, int customerId);
    Task<SlnGiftCardDto?> GetGiftCardByCodeAsync(string code, int customerId);
    Task<SlnGiftCardDto> CreateGiftCardAsync(SlnGiftCardCreateDto dto, int userId, int customerId);
    Task<(bool Success, string? Error)> RedeemGiftCardAsync(SlnGiftCardRedeemDto dto, int customerId);
    Task<(bool Success, string? Error)> DeactivateGiftCardAsync(int id, int customerId);
}
