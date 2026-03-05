using System.Net.Http.Json;

namespace CallCenter.PbxService.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(IHttpClientFactory httpClientFactory, ILogger<ApiClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CallCenterApi");
        _logger = logger;
    }

    public async Task<List<TrunkInfo>> GetSipAccountsAsync(string customerUid)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<TrunkInfo>>(
                $"/api/pbx/trunks?customerUid={customerUid}");
            return result ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SIP hesaplari alinamadi (CustomerUid: {Uid})", customerUid);
            return [];
        }
    }

    public async Task<CustomerPbxConfig?> GetCustomerPbxConfigAsync(string customerUid)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CustomerPbxConfig>(
                $"/api/pbx/config?customerUid={customerUid}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Musteri PBX config alinamadi (CustomerUid: {Uid})", customerUid);
            return null;
        }
    }

    public async Task<BusinessHoursInfo?> GetBusinessHoursAsync(string customerUid)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<BusinessHoursInfo>(
                $"/api/pbx/business-hours?customerUid={customerUid}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mesai saatleri alinamadi (CustomerUid: {Uid})", customerUid);
            return null;
        }
    }

    public async Task<IvrMenuInfo?> GetIvrMenuAsync(string customerUid, int menuId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<IvrMenuInfo>(
                $"/api/pbx/ivr/{menuId}?customerUid={customerUid}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IVR menu alinamadi: {MenuId}", menuId);
            return null;
        }
    }

    public async Task<QueueConfigInfo?> GetQueueConfigAsync(int queueId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<QueueConfigInfo>(
                $"/api/pbx/queues/{queueId}/config");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kuyruk config alinamadi: {QueueId}", queueId);
            return null;
        }
    }

    public async Task<AvailableAgentInfo?> GetAvailableAgentAsync(int queueId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/pbx/queues/{queueId}/available-agent");
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent ||
                response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AvailableAgentInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Musait agent sorgulanamadi: QueueId={QueueId}", queueId);
            return null;
        }
    }

    public async Task<int?> CreateIncomingCallRecordAsync(IncomingCallRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/pbx/calls/incoming", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CallRecordCreated>();
                return result?.Id;
            }
            _logger.LogError("Cagri kaydi olusturulamadi: {Status}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cagri kaydi olusturma hatasi");
            return null;
        }
    }

    public async Task UpdateCallRecordAsync(int callRecordId, CallRecordUpdate update)
    {
        try
        {
            await _httpClient.PutAsJsonAsync($"/api/pbx/calls/{callRecordId}", update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cagri kaydi guncelleme hatasi: {Id}", callRecordId);
        }
    }

    private class CallRecordCreated { public int Id { get; set; } }
}
