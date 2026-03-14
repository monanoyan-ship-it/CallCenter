using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;

namespace CallCenter.Api.Services.Connectors;

/// <summary>
/// Zendesk Support connector.
/// REST API v2 ile kullanici/ticket arama, Talk Partner Edition cagri loglama.
/// API token veya OAuth2 authentication.
/// </summary>
public class ZendeskConnectorAdapter : IConnectorAdapter
{
    private readonly ILogger<ZendeskConnectorAdapter> _logger;

    public int PlatformTypeId => IntegrationPlatforms.Ids.Zendesk;

    public ZendeskConnectorAdapter(ILogger<ZendeskConnectorAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(Dictionary<string, string> credentials)
    {
        if (!TryGetAuth(credentials, out var subdomain, out var authHeader))
            return new ConnectionTestResult { Success = false, Error = "Subdomain, Email ve ApiToken gerekli." };

        try
        {
            using var http = CreateClient(authHeader);
            var response = await http.GetAsync($"https://{subdomain}.zendesk.com/api/v2/users/me.json");
            if (response.IsSuccessStatusCode)
                return new ConnectionTestResult { Success = true, PlatformInfo = "Zendesk baglantisi basarili." };

            return new ConnectionTestResult { Success = false, Error = $"Zendesk API hatasi: {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult { Success = false, Error = $"Baglanti hatasi: {ex.Message}" };
        }
    }

    public async Task<ExternalContactDto?> LookupContactAsync(Dictionary<string, string> credentials, string phoneNumber)
    {
        if (!TryGetAuth(credentials, out var subdomain, out var authHeader)) return null;

        try
        {
            using var http = CreateClient(authHeader);
            var cleanPhone = phoneNumber.Replace("+", "").Replace(" ", "").Replace("-", "");

            var response = await http.GetAsync(
                $"https://{subdomain}.zendesk.com/api/v2/search.json?query=type:user+phone:*{cleanPhone}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var count = doc.RootElement.GetProperty("count").GetInt32();
            if (count == 0) return null;

            var user = doc.RootElement.GetProperty("results")[0];
            var userId = user.GetProperty("id").GetInt64();

            return new ExternalContactDto
            {
                ExternalId = userId.ToString(),
                FullName = user.GetProperty("name").GetString() ?? "",
                Phone = user.TryGetProperty("phone", out var p) ? p.GetString() : null,
                Email = user.TryGetProperty("email", out var e) ? e.GetString() : null,
                Company = user.TryGetProperty("organization_id", out var o) && o.ValueKind != JsonValueKind.Null
                    ? $"Org:{o.GetInt64()}" : null,
                ExternalUrl = $"https://{subdomain}.zendesk.com/agent/users/{userId}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zendesk contact lookup hatasi: {Phone}", phoneNumber);
            return null;
        }
    }

    public async Task<(bool Success, string? Error)> LogCallActivityAsync(Dictionary<string, string> credentials, CallActivityPayload payload)
    {
        if (!TryGetAuth(credentials, out var subdomain, out var authHeader))
            return (false, "Zendesk credential bilgileri eksik.");

        try
        {
            using var http = CreateClient(authHeader);

            // Talk Partner Edition — cagri kaydini ticket comment olarak ekle
            // Veya yeni ticket olustur
            var ticket = new
            {
                ticket = new
                {
                    subject = $"{(payload.Direction == "Inbound" ? "Gelen" : "Giden")} Arama — {payload.CallerNumber}",
                    comment = new
                    {
                        body = $"Agent: {payload.AgentName}\nSure: {payload.DurationSeconds / 60}dk {payload.DurationSeconds % 60}sn\n" +
                               $"Baslangic: {payload.StartedAt:dd.MM.yyyy HH:mm}\nBitis: {payload.EndedAt:dd.MM.yyyy HH:mm}" +
                               (payload.Notes != null ? $"\nNotlar: {payload.Notes}" : ""),
                        @public = false
                    },
                    tags = new[] { "phone-call", payload.Direction.ToLowerInvariant() },
                    requester_id = !string.IsNullOrEmpty(payload.ExternalContactId) && long.TryParse(payload.ExternalContactId, out var reqId) ? reqId : (long?)null,
                    priority = "normal"
                }
            };

            var json = JsonSerializer.Serialize(ticket, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync($"https://{subdomain}.zendesk.com/api/v2/tickets.json", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Zendesk ticket olusturuldu: {CallUid}", payload.CallUid);
                return (true, null);
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            return (false, $"Zendesk ticket olusturma hatasi: {response.StatusCode} — {errorBody}");
        }
        catch (Exception ex)
        {
            return (false, $"Zendesk baglanti hatasi: {ex.Message}");
        }
    }

    public async Task<List<ExternalContactDto>> GetContactsAsync(Dictionary<string, string> credentials, int limit = 100, int offset = 0)
    {
        if (!TryGetAuth(credentials, out var subdomain, out var authHeader)) return new();

        try
        {
            using var http = CreateClient(authHeader);
            var page = (offset / limit) + 1;
            var response = await http.GetAsync(
                $"https://{subdomain}.zendesk.com/api/v2/users.json?page={page}&per_page={limit}&role=end-user");
            if (!response.IsSuccessStatusCode) return new();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var users = doc.RootElement.GetProperty("users");

            var contacts = new List<ExternalContactDto>();
            foreach (var user in users.EnumerateArray())
            {
                var userId = user.GetProperty("id").GetInt64();
                contacts.Add(new ExternalContactDto
                {
                    ExternalId = userId.ToString(),
                    FullName = user.GetProperty("name").GetString() ?? "",
                    Phone = user.TryGetProperty("phone", out var p) ? p.GetString() : null,
                    Email = user.TryGetProperty("email", out var e) ? e.GetString() : null,
                    ExternalUrl = $"https://{subdomain}.zendesk.com/agent/users/{userId}"
                });
            }
            return contacts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zendesk contact listesi hatasi.");
            return new();
        }
    }

    public Task<Dictionary<string, string>?> RefreshTokenAsync(Dictionary<string, string> credentials)
    {
        // Zendesk API token suresi dolmaz — refresh gerekmez
        return Task.FromResult<Dictionary<string, string>?>(null);
    }

    // ─── Helpers ───

    private static bool TryGetAuth(Dictionary<string, string> credentials, out string subdomain, out string authHeader)
    {
        subdomain = "";
        authHeader = "";

        if (!credentials.TryGetValue("Subdomain", out var sub) ||
            !credentials.TryGetValue("Email", out var email) ||
            !credentials.TryGetValue("ApiToken", out var token))
            return false;

        subdomain = sub;
        var authBytes = Encoding.ASCII.GetBytes($"{email}/token:{token}");
        authHeader = Convert.ToBase64String(authBytes);
        return true;
    }

    private static HttpClient CreateClient(string basicAuth)
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
        return http;
    }
}
