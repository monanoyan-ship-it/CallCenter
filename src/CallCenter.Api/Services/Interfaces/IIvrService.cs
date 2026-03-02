using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Interfaces;

public interface IIvrService
{
    // ─── Greeting Messages ───
    Task<List<GreetingMessageDto>> GetGreetingsAsync(int customerId);
    Task<GreetingMessageDto?> GetGreetingAsync(int id, int customerId);
    Task<GreetingMessageDto> CreateGreetingAsync(int customerId, CreateGreetingMessageRequest request, string audioFilePath, string? audioFileName);
    Task<(bool Success, string? Error)> UpdateGreetingAsync(int id, int customerId, UpdateGreetingMessageRequest request);
    Task<(bool Success, string? Error)> DeleteGreetingAsync(int id, int customerId);

    // ─── IVR Menus ───
    Task<List<IvrMenuDto>> GetIvrMenusAsync(int customerId);
    Task<IvrMenuDto?> GetIvrMenuAsync(int id, int customerId);
    Task<IvrMenuDto> CreateIvrMenuAsync(int customerId, CreateIvrMenuRequest request);
    Task<(bool Success, string? Error)> UpdateIvrMenuAsync(int id, int customerId, UpdateIvrMenuRequest request);
    Task<(bool Success, string? Error)> DeleteIvrMenuAsync(int id, int customerId);

    // ─── Hold Music ───
    Task<List<HoldMusicDto>> GetHoldMusicsAsync(int customerId);
    Task<HoldMusicDto> CreateHoldMusicAsync(int customerId, CreateHoldMusicRequest request, string audioFilePath, string? audioFileName);
    Task<(bool Success, string? Error)> DeleteHoldMusicAsync(int id, int customerId);

    // ─── Business Hours ───
    Task<List<BusinessHoursDto>> GetBusinessHoursAsync(int customerId);
    Task SetBusinessHoursAsync(int customerId, SetBusinessHoursRequest request);

    // ─── Holidays ───
    Task<List<HolidayDto>> GetHolidaysAsync(int customerId);
    Task<HolidayDto> CreateHolidayAsync(int customerId, CreateHolidayRequest request);
    Task<(bool Success, string? Error)> DeleteHolidayAsync(int id, int customerId);

    // ─── Incoming Call Pipeline ───
    Task<IncomingCallConfigDto> GetIncomingCallConfigAsync(int customerId, int? queueId = null);
}
