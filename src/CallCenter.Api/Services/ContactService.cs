using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class ContactService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ContactService> _logger;

    public ContactService(AppDbContext db, ILogger<ContactService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════
    // CRUD
    // ═══════════════════════════════════════════════════

    public async Task<List<ContactDto>> GetContactsAsync(int userId, int? customerId, string? search, int page = 1, int pageSize = 50)
    {
        var query = _db.Contacts
            .Where(c => c.OwnerUserId == userId || c.OwnerUserId == null);

        if (customerId.HasValue)
            query = query.Where(c => c.CustomerId == customerId || c.CustomerId == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(s) ||
                c.PhoneNumber.Contains(s) ||
                (c.Company != null && c.Company.ToLower().Contains(s)) ||
                (c.Email != null && c.Email.ToLower().Contains(s)));
        }

        var contacts = await query
            .OrderBy(c => c.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return contacts.Select(MapToDto).ToList();
    }

    public async Task<ContactDto?> GetContactAsync(int contactId, int userId)
    {
        var contact = await _db.Contacts.FindAsync(contactId);
        if (contact == null) return null;
        if (contact.OwnerUserId != null && contact.OwnerUserId != userId) return null;
        return MapToDto(contact);
    }

    public async Task<ContactDto> CreateContactAsync(CreateContactRequest req, int userId, int? customerId)
    {
        var contact = new Contact
        {
            FullName = req.FullName,
            PhoneNumber = req.PhoneNumber,
            PhoneNumber2 = req.PhoneNumber2,
            Email = req.Email,
            Company = req.Company,
            Department = req.Department,
            Title = req.Title,
            Notes = req.Notes,
            SourceId = ContactSources.Ids.Manual,
            OwnerUserId = userId,
            CustomerId = customerId
        };

        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync();
        return MapToDto(contact);
    }

    public async Task<(bool Success, string? Error)> UpdateContactAsync(int contactId, UpdateContactRequest req, int userId)
    {
        var contact = await _db.Contacts.FindAsync(contactId);
        if (contact == null) return (false, "Kayit bulunamadi");
        if (contact.OwnerUserId != null && contact.OwnerUserId != userId) return (false, "Yetki yok");

        contact.FullName = req.FullName;
        contact.PhoneNumber = req.PhoneNumber;
        contact.PhoneNumber2 = req.PhoneNumber2;
        contact.Email = req.Email;
        contact.Company = req.Company;
        contact.Department = req.Department;
        contact.Title = req.Title;
        contact.Notes = req.Notes;
        contact.IsFavorite = req.IsFavorite;
        contact.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteContactAsync(int contactId, int userId)
    {
        var contact = await _db.Contacts.FindAsync(contactId);
        if (contact == null) return (false, "Kayit bulunamadi");
        if (contact.OwnerUserId != null && contact.OwnerUserId != userId) return (false, "Yetki yok");

        _db.Contacts.Remove(contact);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ToggleFavoriteAsync(int contactId, int userId)
    {
        var contact = await _db.Contacts.FindAsync(contactId);
        if (contact == null) return (false, "Kayit bulunamadi");

        contact.IsFavorite = !contact.IsFavorite;
        contact.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    // ═══════════════════════════════════════════════════
    // CSV IMPORT
    // ═══════════════════════════════════════════════════

    public async Task<CsvImportResult> ImportFromCsvAsync(CsvImportRequest req, int userId, int? customerId)
    {
        var result = new CsvImportResult();

        try
        {
            var csvBytes = Convert.FromBase64String(req.CsvContentBase64);
            var csvText = System.Text.Encoding.UTF8.GetString(csvBytes);
            var lines = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            int startLine = req.HasHeader ? 1 : 0;
            result.TotalRows = lines.Length - startLine;

            for (int i = startLine; i < lines.Length; i++)
            {
                try
                {
                    var columns = ParseCsvLine(lines[i]);
                    var contact = new Contact
                    {
                        SourceId = ContactSources.Ids.CSV,
                        OwnerUserId = userId,
                        CustomerId = customerId
                    };

                    foreach (var mapping in req.ColumnMapping)
                    {
                        if (mapping.Key >= columns.Length) continue;
                        var value = columns[mapping.Key].Trim();
                        if (string.IsNullOrEmpty(value)) continue;

                        switch (mapping.Value.ToLower())
                        {
                            case "fullname": contact.FullName = value; break;
                            case "phonenumber": contact.PhoneNumber = value; break;
                            case "phonenumber2": contact.PhoneNumber2 = value; break;
                            case "email": contact.Email = value; break;
                            case "company": contact.Company = value; break;
                            case "department": contact.Department = value; break;
                            case "title": contact.Title = value; break;
                            case "notes": contact.Notes = value; break;
                        }
                    }

                    if (string.IsNullOrEmpty(contact.FullName) || string.IsNullOrEmpty(contact.PhoneNumber))
                    {
                        result.SkippedCount++;
                        result.Errors.Add($"Satir {i + 1}: Ad veya telefon numarasi eksik");
                        continue;
                    }

                    _db.Contacts.Add(contact);
                    result.ImportedCount++;
                }
                catch (Exception ex)
                {
                    result.SkippedCount++;
                    result.Errors.Add($"Satir {i + 1}: {ex.Message}");
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("CSV import tamamlandi: {Imported}/{Total} kayit", result.ImportedCount, result.TotalRows);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"CSV parse hatasi: {ex.Message}");
            _logger.LogError(ex, "CSV import basarisiz");
        }

        return result;
    }

    /// <summary>Basit CSV satir parser (tirnak icindeki virgulleri destekler)</summary>
    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    // ═══════════════════════════════════════════════════
    // LDAP SYNC (Stub — System.DirectoryServices gerektirir)
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// LDAP/Active Directory'den rehber senkronizasyonu.
    /// NOT: Gercek implementasyon System.DirectoryServices.Protocols NuGet paketi gerektirir.
    /// Bu stub, interface'i tanimlar ve loglama yapar.
    /// </summary>
    public async Task<LdapSyncResult> SyncFromLdapAsync(LdapConfigDto config, int? customerId)
    {
        var result = new LdapSyncResult();

        _logger.LogInformation("LDAP sync baslatiliyor: Server={Server}, BaseDN={BaseDn}",
            config.Server, config.BaseDn);

        // TODO: System.DirectoryServices.Protocols ile LDAP baglantisi
        // var connection = new LdapConnection(new LdapDirectoryIdentifier(config.Server, config.Port));
        // connection.Credential = new NetworkCredential(config.BindDn, config.BindPassword);
        // connection.SessionOptions.SecureSocketLayer = config.UseSsl;
        //
        // var searchRequest = new SearchRequest(
        //     config.BaseDn,
        //     config.SearchFilter,
        //     SearchScope.Subtree,
        //     "cn", "sn", "givenName", "telephoneNumber", "mobile", "mail", "company", "department", "title"
        // );
        //
        // var searchResponse = (SearchResponse)connection.SendRequest(searchRequest);
        // foreach (SearchResultEntry entry in searchResponse.Entries)
        // {
        //     var contact = new Contact
        //     {
        //         FullName = GetAttribute(entry, "cn"),
        //         PhoneNumber = GetAttribute(entry, "telephoneNumber") ?? GetAttribute(entry, "mobile"),
        //         Email = GetAttribute(entry, "mail"),
        //         Company = GetAttribute(entry, "company"),
        //         Department = GetAttribute(entry, "department"),
        //         Title = GetAttribute(entry, "title"),
        //         SourceId = ContactSources.Ids.LDAP,
        //         LdapDn = entry.DistinguishedName,
        //         CustomerId = customerId
        //     };
        //     // Upsert by LdapDn
        // }

        result.Errors.Add("LDAP entegrasyonu henuz implementasyon asamasinda. System.DirectoryServices.Protocols paketi gereklidir.");
        _logger.LogWarning("LDAP sync: Stub implementasyon, gercek LDAP baglantisi yapilmadi");

        return await Task.FromResult(result);
    }

    // ═══════════════════════════════════════════════════

    private static ContactDto MapToDto(Contact c) => new()
    {
        Id = c.Id,
        Uid = c.Uid,
        FullName = c.FullName,
        PhoneNumber = c.PhoneNumber,
        PhoneNumber2 = c.PhoneNumber2,
        Email = c.Email,
        Company = c.Company,
        Department = c.Department,
        Title = c.Title,
        SourceId = c.SourceId,
        SourceName = ContactSources.GetById(c.SourceId)?.SystemName ?? "Manual",
        IsFavorite = c.IsFavorite
    };
}
