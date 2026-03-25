using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ICrmContactFactory
{
    Task<List<CrmContactSimpleDto>> GetContactsAsync(int userId, int? customerId, string? search, int page = 1, int pageSize = 50);
    Task<CrmContactSimpleDto?> GetContactAsync(int contactId, int userId);
    Task<CrmContactSimpleDto> CreateContactAsync(CreateContactRequest req, int userId, int? customerId);
    Task<(bool Success, string? Error)> UpdateContactAsync(int contactId, UpdateContactRequest req, int userId);
    Task<(bool Success, string? Error)> DeleteContactAsync(int contactId, int userId);
    Task<(bool Success, string? Error)> ToggleFavoriteAsync(int contactId, int userId);
    Task<CsvImportResult> ImportFromCsvAsync(CsvImportRequest req, int userId, int? customerId);
    Task<LdapSyncResult> SyncFromLdapAsync(LdapConfigDto config, int? customerId);
    FileParseResult ParseFile(FileParseRequest req);
    Task<BulkImportResult> BulkImportAsync(BulkImportRequest req, int userId, int? customerId);
}
