using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IIvrFactory
{
    Task<List<GreetingMessageDto>> GetGreetingsAsync(int customerId);
    Task<GreetingMessageDto?> GetGreetingAsync(int id, int customerId);
    Task<GreetingMessageDto> CreateGreetingAsync(int customerId, CreateGreetingMessageRequest request, string? audioFilePath, string? audioFileName);
    Task<(bool, string?)> UpdateGreetingAsync(int id, int customerId, UpdateGreetingMessageRequest request);
    Task<(bool, string?)> DeleteGreetingAsync(int id, int customerId);
    Task<List<IvrMenuDto>> GetIvrMenusAsync(int customerId);
    Task<IvrMenuDto?> GetIvrMenuAsync(int id, int customerId);
    Task<IvrMenuDto> CreateIvrMenuAsync(int customerId, CreateIvrMenuRequest request);
    Task<(bool, string?)> UpdateIvrMenuAsync(int id, int customerId, UpdateIvrMenuRequest request);
    Task<(bool, string?)> DeleteIvrMenuAsync(int id, int customerId);
    Task<List<HoldMusicDto>> GetHoldMusicsAsync(int customerId);
    Task<HoldMusicDto> CreateHoldMusicAsync(int customerId, CreateHoldMusicRequest request, string audioFilePath, string? audioFileName);
    Task<(bool, string?)> DeleteHoldMusicAsync(int id, int customerId);
    Task<List<BusinessHoursDto>> GetBusinessHoursAsync(int customerId);
    Task SetBusinessHoursAsync(int customerId, SetBusinessHoursRequest request);
    Task<List<HolidayDto>> GetHolidaysAsync(int customerId);
    Task<HolidayDto> CreateHolidayAsync(int customerId, CreateHolidayRequest request);
    Task<(bool, string?)> DeleteHolidayAsync(int id, int customerId);
    Task<IncomingCallConfigDto> GetIncomingCallConfigAsync(int customerId, int? queueId = null);
    Task<List<Shared.Services.TtsLanguageInfo>> GetTtsLanguagesAsync();
    Task<List<Shared.Services.TtsVoiceInfo>> GetTtsVoicesAsync(string language);
}
