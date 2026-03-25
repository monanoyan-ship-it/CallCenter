using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;

namespace CallCenter.Api.Services.Connectors;

/// <summary>
/// Salesforce CRM connector.
/// REST API v62.0 ile CrmContact/Lead arama, Task olusturma, SOQL sorgulari.
/// OAuth2 token refresh destegi.
/// </summary>
public class SalesforceConnectorAdapter : IConnectorAdapter
{
    private readonly ILogger<SalesforceConnectorAdapter> _logger;
    private const string ApiVersion = "v62.0";

    public int PlatformTypeId => IntegrationPlatforms.Ids.Salesforce;

    public SalesforceConnectorAdapter(ILogger<SalesforceConnectorAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(Dictionary<string, string> credentials)
    {
        if (!credentials.TryGetValue("InstanceUrl", out var instanceUrl) ||
            !credentials.TryGetValue("AccessToken", out var accessToken))
            return new ConnectionTestResult { Success = false, Error = "InstanceUrl ve AccessToken gerekli." };

        try
        {
            using var http = CreateClient(accessToken);
            var response = await http.GetAsync($"{instanceUrl}/services/data/{ApiVersion}/");
            if (response.IsSuccessStatusCode)
                return new ConnectionTestResult { Success = true, PlatformInfo = $"Salesforce API {ApiVersion} baglantisi basarili." };

            var body = await response.Content.ReadAsStringAsync();
            return new ConnectionTestResult { Success = false, Error = $"Salesforce API hatasi: {response.StatusCode} — {body}" };
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult { Success = false, Error = $"Baglanti hatasi: {ex.Message}" };
        }
    }

    public async Task<ExternalContactDto?> LookupContactAsync(Dictionary<string, string> credentials, string phoneNumber)
    {
        var (instanceUrl, accessToken) = GetAuth(credentials);
        if (instanceUrl == null || accessToken == null) return null;

        try
        {
            using var http = CreateClient(accessToken);

            // Telefon numarasini temizle (+ isareti vb.)
            var cleanPhone = phoneNumber.Replace("+", "").Replace(" ", "").Replace("-", "");

            // Once CrmContact'ta ara, sonra Lead'de
            var soql = $"SELECT Id, Name, Phone, Email, Account.Name FROM CrmContact WHERE Phone LIKE '%{cleanPhone}%' LIMIT 1";
            var result = await QueryAsync(http, instanceUrl, soql);

            if (result != null) return result;

            // Lead'de ara
            soql = $"SELECT Id, Name, Phone, Email, Company FROM Lead WHERE Phone LIKE '%{cleanPhone}%' LIMIT 1";
            return await QueryAsync(http, instanceUrl, soql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Salesforce contact lookup hatasi: {Phone}", phoneNumber);
            return null;
        }
    }

    public async Task<(bool Success, string? Error)> LogCallActivityAsync(Dictionary<string, string> credentials, CallActivityPayload payload)
    {
        var (instanceUrl, accessToken) = GetAuth(credentials);
        if (instanceUrl == null || accessToken == null)
            return (false, "Salesforce credential bilgileri eksik.");

        try
        {
            using var http = CreateClient(accessToken);

            // Salesforce Task SObject olarak cagri aktivitesi olustur
            var task = new
            {
                Subject = $"{(payload.Direction == "Inbound" ? "Gelen" : "Giden")} Arama — {payload.CallerNumber}",
                Status = "Completed",
                Priority = "Normal",
                TaskSubtype = "Call",
                CallType = payload.Direction == "Inbound" ? "Inbound" : "Outbound",
                CallDurationInSeconds = payload.DurationSeconds,
                Description = BuildCallDescription(payload),
                WhoId = payload.ExternalContactId, // Salesforce CrmContact/Lead ID (varsa)
                ActivityDate = payload.StartedAt.ToString("yyyy-MM-dd")
            };

            var json = JsonSerializer.Serialize(task);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync($"{instanceUrl}/services/data/{ApiVersion}/sobjects/Task", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Salesforce Task olusturuldu: {CallUid}", payload.CallUid);
                return (true, null);
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            return (false, $"Salesforce Task olusturma hatasi: {response.StatusCode} — {errorBody}");
        }
        catch (Exception ex)
        {
            return (false, $"Salesforce baglanti hatasi: {ex.Message}");
        }
    }

    public async Task<List<ExternalContactDto>> GetContactsAsync(Dictionary<string, string> credentials, int limit = 100, int offset = 0)
    {
        var (instanceUrl, accessToken) = GetAuth(credentials);
        if (instanceUrl == null || accessToken == null) return new();

        try
        {
            using var http = CreateClient(accessToken);
            var soql = $"SELECT Id, Name, Phone, Email, Account.Name FROM CrmContact WHERE Phone != null ORDER BY LastModifiedDate DESC LIMIT {limit} OFFSET {offset}";

            var response = await http.GetAsync($"{instanceUrl}/services/data/{ApiVersion}/query?q={Uri.EscapeDataString(soql)}");
            if (!response.IsSuccessStatusCode) return new();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var records = doc.RootElement.GetProperty("records");

            var contacts = new List<ExternalContactDto>();
            foreach (var record in records.EnumerateArray())
            {
                contacts.Add(new ExternalContactDto
                {
                    ExternalId = record.GetProperty("Id").GetString() ?? "",
                    FullName = record.GetProperty("Name").GetString() ?? "",
                    Phone = record.TryGetProperty("Phone", out var p) ? p.GetString() : null,
                    Email = record.TryGetProperty("Email", out var e) ? e.GetString() : null,
                    Company = record.TryGetProperty("Account", out var a) && a.ValueKind != JsonValueKind.Null
                        ? a.TryGetProperty("Name", out var an) ? an.GetString() : null : null,
                    ExternalUrl = $"{instanceUrl}/{record.GetProperty("Id").GetString()}"
                });
            }

            return contacts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Salesforce contact listesi hatasi.");
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
            var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = refreshToken
            });

            var response = await http.PostAsync("https://login.salesforce.com/services/oauth2/token", requestContent);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var newCreds = new Dictionary<string, string>(credentials)
            {
                ["AccessToken"] = doc.RootElement.GetProperty("access_token").GetString() ?? "",
                ["InstanceUrl"] = doc.RootElement.GetProperty("instance_url").GetString() ?? credentials.GetValueOrDefault("InstanceUrl", ""),
                ["TokenExpiresAt"] = DateTime.UtcNow.AddHours(2).ToString("o")
            };

            _logger.LogInformation("Salesforce token yenilendi.");
            return newCreds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Salesforce token refresh hatasi.");
            return null;
        }
    }

    // ─── Helpers ───

    private static HttpClient CreateClient(string accessToken)
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return http;
    }

    private static (string? InstanceUrl, string? AccessToken) GetAuth(Dictionary<string, string> credentials)
    {
        credentials.TryGetValue("InstanceUrl", out var instanceUrl);
        credentials.TryGetValue("AccessToken", out var accessToken);
        return (instanceUrl, accessToken);
    }

    private async Task<ExternalContactDto?> QueryAsync(HttpClient http, string instanceUrl, string soql)
    {
        var response = await http.GetAsync($"{instanceUrl}/services/data/{ApiVersion}/query?q={Uri.EscapeDataString(soql)}");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var totalSize = doc.RootElement.GetProperty("totalSize").GetInt32();
        if (totalSize == 0) return null;

        var record = doc.RootElement.GetProperty("records")[0];
        return new ExternalContactDto
        {
            ExternalId = record.GetProperty("Id").GetString() ?? "",
            FullName = record.GetProperty("Name").GetString() ?? "",
            Phone = record.TryGetProperty("Phone", out var p) ? p.GetString() : null,
            Email = record.TryGetProperty("Email", out var e) ? e.GetString() : null,
            ExternalUrl = $"{instanceUrl}/{record.GetProperty("Id").GetString()}"
        };
    }

    private static string BuildCallDescription(CallActivityPayload payload)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Agent: {payload.AgentName}");
        if (!string.IsNullOrEmpty(payload.QueueName)) sb.AppendLine($"Kuyruk: {payload.QueueName}");
        sb.AppendLine($"Sure: {payload.DurationSeconds / 60}dk {payload.DurationSeconds % 60}sn");
        sb.AppendLine($"Baslangic: {payload.StartedAt:dd.MM.yyyy HH:mm}");
        sb.AppendLine($"Bitis: {payload.EndedAt:dd.MM.yyyy HH:mm}");
        if (!string.IsNullOrEmpty(payload.Notes)) sb.AppendLine($"Notlar: {payload.Notes}");
        return sb.ToString();
    }
}
