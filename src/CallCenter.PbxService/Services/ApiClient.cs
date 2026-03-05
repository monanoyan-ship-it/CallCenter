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
            // TODO 11.10: API'ye PbxService icin ozel endpoint eklenecek
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
}
