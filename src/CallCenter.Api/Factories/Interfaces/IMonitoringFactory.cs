using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IMonitoringFactory
{
    Task<MonitoringSessionDto> StartMonitoringAsync(StartMonitoringRequest req, int supervisorId);
    Task<(bool Success, string? Error)> ChangeModeAsync(int sessionId, int newModeId);
    Task<(bool Success, string? Error)> StopMonitoringAsync(int sessionId);
    Task<List<MonitoringSessionDto>> GetActiveSessionsAsync(int? customerId);
    Task<List<MonitorableCallDto>> GetMonitorableCallsAsync(int? customerId);
}
