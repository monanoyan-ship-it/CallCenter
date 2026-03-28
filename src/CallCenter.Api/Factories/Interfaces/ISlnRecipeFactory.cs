using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnRecipeFactory
{
    Task<List<SlnRecipeDto>> GetRecipesAsync(int customerId);
    Task<SlnRecipeDto?> GetRecipeAsync(int id, int customerId);
    Task<SlnRecipeDto> CreateRecipeAsync(SlnRecipeCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateRecipeAsync(int id, SlnRecipeCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeleteRecipeAsync(int id, int customerId);
}
