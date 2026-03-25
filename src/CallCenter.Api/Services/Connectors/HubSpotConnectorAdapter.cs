using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;

namespace CallCenter.Api.Services.Connectors;

/// <summary>
/// HubSpot CRM connector.
/// CRM v3 API ile CrmContact arama, Call engagement olusturma.
/// OAuth2 token refresh destegi.
/// </summary>
public class HubSpotConnectorAdapter : IConnectorAdapter
{
    private readonly ILogger<HubSpotConnectorAdapter> _logger;
    private const string BaseUrl = "https://api.hubapi.com";

    public int PlatformTypeId => IntegrationPlatforms.Ids.HubSpot;

    public HubSpotConnectorAdapter(ILogger<HubSpotConnectorAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(Dictionary<string, string> credentials)
    {
        if (!credentials.TryGetValue("AccessToken", out var accessToken))
            return new ConnectionTestResult { Success = false, Error = "AccessToken gerekli." };

        try
        {
            using var http = CreateClient(accessToken);
            var response = await http.GetAsync($"{BaseUrl}/crm/v3/objects/contacts?limit=1");
            if (response.IsSuccessStatusCode)
                return new ConnectionTestResult { Success = true, PlatformInfo = "HubSpot CRM baglantisi basarili." };

            return new ConnectionTestResult { Success = false, Error = $"HubSpot API hatasi: {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult { Success = false, Error = $"Baglanti hatasi: {ex.Message}" };
        }
    }

    public async Task<ExternalContactDto?> LookupContactAsync(Dictionary<string, string> credentials, string phoneNumber)
    {
        if (!credentials.TryGetValue("AccessToken", out var accessToken)) return null;

        try
        {
            using var http = CreateClient(accessToken);
            var cleanPhone = phoneNumber.Replace("+", "").Replace(" ", "").Replace("-", "");

            var searchBody = new
            {
                filterGroups = new[]
                {
                    new
                    {
                        filters = new[]
                        {
                            new { propertyName = "phone", @operator = "CONTAINS_TOKEN", value = $"*{cleanPhone}" }
                        }
                    }
                },
                properties = new[] { "firstname", "lastname", "phone", "email", "company" },
                limit = 1
            };

            var json = JsonSerializer.Serialize(searchBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync($"{BaseUrl}/crm/v3/objects/contacts/search", content);

            if (!response.IsSuccessStatusCode) return null;

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var total = doc.RootElement.GetProperty("total").GetInt32();
            if (total == 0) return null;

            var result = doc.RootElement.GetProperty("results")[0];
            var props = result.GetProperty("properties");

            var firstName = props.TryGetProperty("firstname", out var fn) ? fn.GetString() ?? "" : "";
            var lastName = props.TryGetProperty("lastname", out var ln) ? ln.GetString() ?? "" : "";
            var contactId = result.GetProperty("id").GetString() ?? "";
            var portalId = credentials.GetValueOrDefault("PortalId", "");

            return new ExternalContactDto
            {
                ExternalId = contactId,
                FullName = $"{firstName} {lastName}".Trim(),
                Phone = props.TryGetProperty("phone", out var p) ? p.GetString() : null,
                Email = props.TryGetProperty("email", out var e) ? e.GetString() : null,
                Company = props.TryGetProperty("company", out var c) ? c.GetString() : null,
                ExternalUrl = $"https://app.hubspot.com/contacts/{portalId}/contact/{contactId}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HubSpot contact lookup hatasi: {Phone}", phoneNumber);
            return null;
        }
    }

    public async Task<(bool Success, string? Error)> LogCallActivityAsync(Dictionary<string, string> credentials, CallActivityPayload payload)
    {
        if (!credentials.TryGetValue("AccessToken", out var accessToken))
            return (false, "AccessToken eksik.");

        try
        {
            using var http = CreateClient(accessToken);

            var callObj = new
            {
                properties = new Dictionary<string, object>
                {
                    ["hs_call_title"] = $"{(payload.Direction == "Inbound" ? "Gelen" : "Giden")} Arama",
                    ["hs_call_body"] = $"Agent: {payload.AgentName}, Sure: {payload.DurationSeconds}sn{(payload.Notes != null ? $", Not: {payload.Notes}" : "")}",
                    ["hs_call_direction"] = payload.Direction == "Inbound" ? "INBOUND" : "OUTBOUND",
                    ["hs_call_duration"] = payload.DurationSeconds * 1000, // ms
                    ["hs_call_from_number"] = payload.CallerNumber,
                    ["hs_call_to_number"] = payload.CalleeNumber,
                    ["hs_call_status"] = "COMPLETED",
                    ["hs_timestamp"] = payload.StartedAt.ToString("o")
                }
            };

            var json = JsonSerializer.Serialize(callObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync($"{BaseUrl}/crm/v3/objects/calls", content);

            if (response.IsSuccessStatusCode)
            {
                // CrmContact ile iliskilendir (varsa)
                if (!string.IsNullOrEmpty(payload.ExternalContactId))
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    var callId = doc.RootElement.GetProperty("id").GetString();

                    if (callId != null)
                    {
                        var assocResponse = await http.PutAsync(
                            $"{BaseUrl}/crm/v3/objects/calls/{callId}/associations/contacts/{payload.ExternalContactId}/194",
                            new StringContent("{}", Encoding.UTF8, "application/json"));
                    }
                }

                _logger.LogInformation("HubSpot Call olusturuldu: {CallUid}", payload.CallUid);
                return (true, null);
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            return (false, $"HubSpot Call olusturma hatasi: {response.StatusCode} — {errorBody}");
        }
        catch (Exception ex)
        {
            return (false, $"HubSpot baglanti hatasi: {ex.Message}");
        }
    }

    public async Task<List<ExternalContactDto>> GetContactsAsync(Dictionary<string, string> credentials, int limit = 100, int offset = 0)
    {
        if (!credentials.TryGetValue("AccessToken", out var accessToken)) return new();

        try
        {
            using var http = CreateClient(accessToken);
            var response = await http.GetAsync(
                $"{BaseUrl}/crm/v3/objects/contacts?limit={limit}&properties=firstname,lastname,phone,email,company");

            if (!response.IsSuccessStatusCode) return new();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var results = doc.RootElement.GetProperty("results");
            var portalId = credentials.GetValueOrDefault("PortalId", "");

            var contacts = new List<ExternalContactDto>();
            foreach (var result in results.EnumerateArray())
            {
                var props = result.GetProperty("properties");
                var firstName = props.TryGetProperty("firstname", out var fn) ? fn.GetString() ?? "" : "";
                var lastName = props.TryGetProperty("lastname", out var ln) ? ln.GetString() ?? "" : "";
                var id = result.GetProperty("id").GetString() ?? "";

                contacts.Add(new ExternalContactDto
                {
                    ExternalId = id,
                    FullName = $"{firstName} {lastName}".Trim(),
                    Phone = props.TryGetProperty("phone", out var p) ? p.GetString() : null,
                    Email = props.TryGetProperty("email", out var e) ? e.GetString() : null,
                    Company = props.TryGetProperty("company", out var c) ? c.GetString() : null,
                    ExternalUrl = $"https://app.hubspot.com/contacts/{portalId}/contact/{id}"
                });
            }
            return contacts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HubSpot contact listesi hatasi.");
            return new();
        }
    }

    public async Task<Dictionary<string, string>?> RefreshTokenAsync(Dictionary<string, string> credentials)
    {
        if (!credentials.TryGetValue("RefreshToken", out var refreshToken) ||
            !credentials.TryGetValue("ClientId", out var clientId) ||
            !credentials.TryGetValue("ClientSecret", out var clientSecret))
            return null;

        try
        {
            using var http = new HttpClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = refreshToken
            });

            var response = await http.PostAsync("https://api.hubapi.com/oauth/v1/token", content);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return new Dictionary<string, string>(credentials)
            {
                ["AccessToken"] = doc.RootElement.GetProperty("access_token").GetString() ?? "",
                ["RefreshToken"] = doc.RootElement.GetProperty("refresh_token").GetString() ?? refreshToken,
                ["TokenExpiresAt"] = DateTime.UtcNow.AddSeconds(doc.RootElement.GetProperty("expires_in").GetInt32()).ToString("o")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HubSpot token refresh hatasi.");
            return null;
        }
    }

    private static HttpClient CreateClient(string accessToken)
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return http;
    }
}
