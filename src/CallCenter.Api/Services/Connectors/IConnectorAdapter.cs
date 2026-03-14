using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Connectors;

/// <summary>
/// Platform-specific connector interface.
/// Her CRM platformu (Salesforce, HubSpot, Zendesk) bu interface'i implemente eder.
/// ConnectorFactory strategy pattern ile dogru adapter'i secer.
/// </summary>
public interface IConnectorAdapter
{
    /// <summary>Bu adapter'in destekledigi platform tipi (IntegrationPlatforms.Ids)</summary>
    int PlatformTypeId { get; }

    /// <summary>Baglanti testi — credential'lar gecerli mi?</summary>
    Task<ConnectionTestResult> TestConnectionAsync(Dictionary<string, string> credentials);

    /// <summary>Telefon numarasi ile dis sistemde kisi ara</summary>
    Task<ExternalContactDto?> LookupContactAsync(Dictionary<string, string> credentials, string phoneNumber);

    /// <summary>Cagri aktivitesini dis sisteme logla (Task/Activity olustur)</summary>
    Task<(bool Success, string? Error)> LogCallActivityAsync(Dictionary<string, string> credentials, CallActivityPayload payload);

    /// <summary>Dis sistemden kisi listesi cek (sync icin)</summary>
    Task<List<ExternalContactDto>> GetContactsAsync(Dictionary<string, string> credentials, int limit = 100, int offset = 0);

    /// <summary>OAuth2 token yenileme (access token expire oldugunda)</summary>
    Task<Dictionary<string, string>?> RefreshTokenAsync(Dictionary<string, string> credentials);
}
