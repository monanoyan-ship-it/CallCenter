using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnConsentFormFactory
{
    Task<List<SlnConsentFormDto>> GetFormsAsync(int customerId);
    Task<SlnConsentFormDto?> GetFormAsync(int id, int customerId);
    Task<SlnConsentFormDto> CreateFormAsync(SlnConsentFormCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateFormAsync(int id, SlnConsentFormUpdateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeleteFormAsync(int id, int customerId);
    Task<List<SlnClientConsentDto>> GetSignedConsentsAsync(int customerId, int? formId = null);
    Task<SlnClientConsentDto> CreateConsentAsync(SlnClientConsentCreateDto dto);
}
