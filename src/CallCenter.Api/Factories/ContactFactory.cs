using System.IO.Compression;
using System.Xml.Linq;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class ContactFactory : IContactFactory
{
    private readonly IContactEntityService _contacts;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ContactFactory> _logger;

    public ContactFactory(IContactEntityService contacts, IUnitOfWork uow, ILogger<ContactFactory> logger)
    {
        _contacts = contacts;
        _uow = uow;
        _logger = logger;
    }

    public async Task<List<ContactDto>> GetContactsAsync(int userId, int? customerId, string? search, int page = 1, int pageSize = 50)
    {
        var query = _contacts.GetAllQueryable();

        // Ayni firmadaki tum personel ortak rehberi gorur
        if (customerId.HasValue)
            query = query.Where(c => c.CustomerId == customerId);
        else
            query = query.Where(c => c.OwnerUserId == userId || c.OwnerUserId == null);

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
        var contact = await _contacts.GetByIdAsync(contactId);
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

        _contacts.Add(contact);
        await _uow.SaveChangesAsync();
        return MapToDto(contact);
    }

    public async Task<(bool Success, string? Error)> UpdateContactAsync(int contactId, UpdateContactRequest req, int userId)
    {
        var contact = await _contacts.GetByIdAsync(contactId);
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

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteContactAsync(int contactId, int userId)
    {
        var contact = await _contacts.GetByIdAsync(contactId);
        if (contact == null) return (false, "Kayit bulunamadi");
        if (contact.OwnerUserId != null && contact.OwnerUserId != userId) return (false, "Yetki yok");

        _contacts.Remove(contact);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ToggleFavoriteAsync(int contactId, int userId)
    {
        var contact = await _contacts.GetByIdAsync(contactId);
        if (contact == null) return (false, "Kayit bulunamadi");

        contact.IsFavorite = !contact.IsFavorite;
        contact.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        return (true, null);
    }

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

                    _contacts.Add(contact);
                    result.ImportedCount++;
                }
                catch (Exception ex)
                {
                    result.SkippedCount++;
                    result.Errors.Add($"Satir {i + 1}: {ex.Message}");
                }
            }

            await _uow.SaveChangesAsync();
            _logger.LogInformation("CSV import tamamlandi: {Imported}/{Total} kayit", result.ImportedCount, result.TotalRows);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"CSV parse hatasi: {ex.Message}");
            _logger.LogError(ex, "CSV import basarisiz");
        }

        return result;
    }

    public async Task<LdapSyncResult> SyncFromLdapAsync(LdapConfigDto config, int? customerId)
    {
        var result = new LdapSyncResult();

        _logger.LogInformation("LDAP sync baslatiliyor: Server={Server}, BaseDN={BaseDn}",
            config.Server, config.BaseDn);

        result.Errors.Add("LDAP entegrasyonu henuz implementasyon asamasinda. System.DirectoryServices.Protocols paketi gereklidir.");
        _logger.LogWarning("LDAP sync: Stub implementasyon, gercek LDAP baglantisi yapilmadi");

        return await Task.FromResult(result);
    }

    public FileParseResult ParseFile(FileParseRequest req)
    {
        var result = new FileParseResult();
        try
        {
            var bytes = Convert.FromBase64String(req.FileContentBase64);
            var ext = Path.GetExtension(req.FileName).ToLowerInvariant();

            switch (ext)
            {
                case ".xlsx":
                case ".xls":
                    ParseExcel(bytes, result);
                    break;
                case ".docx":
                    ParseDocx(bytes, result);
                    break;
                default:
                    result.Errors.Add($"Desteklenmeyen dosya tipi: {ext}");
                    break;
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Dosya parse hatasi: {ex.Message}");
            _logger.LogError(ex, "File parse failed: {FileName}", req.FileName);
        }
        return result;
    }

    public async Task<BulkImportResult> BulkImportAsync(BulkImportRequest req, int userId, int? customerId)
    {
        var result = new BulkImportResult { TotalRows = req.Contacts.Count };

        // Mevcut numaralari al (duplicate kontrolu)
        var existingPhones = await _contacts.GetAllQueryable()
            .Where(c => c.CustomerId == customerId)
            .Select(c => c.PhoneNumber)
            .ToListAsync();
        var existingSet = new HashSet<string>(existingPhones);

        foreach (var item in req.Contacts)
        {
            if (!item.IsValid)
            {
                result.SkippedCount++;
                continue;
            }

            var phone = SanitizePhone(item.PhoneNumber);
            if (string.IsNullOrEmpty(phone))
            {
                result.SkippedCount++;
                result.Errors.Add($"Gecersiz numara: {item.PhoneNumber}");
                continue;
            }

            if (existingSet.Contains(phone))
            {
                result.DuplicateCount++;
                continue;
            }

            var contact = new Contact
            {
                FullName = item.FullName.Trim(),
                PhoneNumber = phone,
                Email = item.Email?.Trim(),
                Company = item.Company?.Trim(),
                Notes = item.Notes?.Trim(),
                SourceId = ContactSources.Ids.CSV,
                OwnerUserId = userId,
                CustomerId = customerId
            };

            _contacts.Add(contact);
            existingSet.Add(phone);
            result.ImportedCount++;
        }

        if (result.ImportedCount > 0)
            await _uow.SaveChangesAsync();

        _logger.LogInformation("Bulk import: {Imported}/{Total} kayit (duplicate: {Dup}, skip: {Skip})",
            result.ImportedCount, result.TotalRows, result.DuplicateCount, result.SkippedCount);

        return result;
    }

    private void ParseExcel(byte[] bytes, FileParseResult result)
    {
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        // SharedStrings (text values stored separately)
        var sharedStrings = new List<string>();
        var ssEntry = archive.GetEntry("xl/sharedStrings.xml");
        if (ssEntry != null)
        {
            using var ssStream = ssEntry.Open();
            var ssDoc = XDocument.Load(ssStream);
            var ns = ssDoc.Root!.GetDefaultNamespace();
            foreach (var si in ssDoc.Descendants(ns + "si"))
            {
                sharedStrings.Add(string.Concat(si.Descendants(ns + "t").Select(t => t.Value)));
            }
        }

        // Read sheet1
        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
        if (sheetEntry == null)
        {
            result.Errors.Add("Excel dosyasinda sheet1 bulunamadi.");
            return;
        }

        using var sheetStream = sheetEntry.Open();
        var sheetDoc = XDocument.Load(sheetStream);
        var sheetNs = sheetDoc.Root!.GetDefaultNamespace();

        var rows = sheetDoc.Descendants(sheetNs + "row").ToList();
        if (rows.Count == 0) return;

        // First row = headers
        var headerRow = rows[0];
        var headers = new List<string>();
        foreach (var cell in headerRow.Elements(sheetNs + "c"))
        {
            headers.Add(GetCellValue(cell, sheetNs, sharedStrings));
        }
        result.Headers = headers;

        // Auto-detect column mapping
        int nameCol = -1, phoneCol = -1, companyCol = -1, emailCol = -1;
        for (int i = 0; i < headers.Count; i++)
        {
            var h = headers[i].ToLowerInvariant().Trim();
            if (nameCol < 0 && IsNameHeader(h)) nameCol = i;
            else if (phoneCol < 0 && IsPhoneHeader(h)) phoneCol = i;
            else if (companyCol < 0 && IsCompanyHeader(h)) companyCol = i;
            else if (emailCol < 0 && IsEmailHeader(h)) emailCol = i;
        }

        // Data rows
        for (int r = 1; r < rows.Count; r++)
        {
            var cells = rows[r].Elements(sheetNs + "c").ToList();
            var values = new Dictionary<int, string>();

            foreach (var cell in cells)
            {
                var colRef = cell.Attribute("r")?.Value ?? "";
                int colIndex = ColumnRefToIndex(colRef);
                values[colIndex] = GetCellValue(cell, sheetNs, sharedStrings);
            }

            var item = new BulkContactItem();
            if (nameCol >= 0 && values.ContainsKey(nameCol)) item.FullName = values[nameCol];
            if (phoneCol >= 0 && values.ContainsKey(phoneCol)) item.PhoneNumber = values[phoneCol];
            if (companyCol >= 0 && values.ContainsKey(companyCol)) item.Company = values[companyCol];
            if (emailCol >= 0 && values.ContainsKey(emailCol)) item.Email = values[emailCol];

            // Fallback: if no name/phone columns detected, try first two columns
            if (nameCol < 0 && phoneCol < 0 && values.Count >= 2)
            {
                var ordered = values.OrderBy(v => v.Key).ToList();
                item.FullName = ordered[0].Value;
                item.PhoneNumber = ordered.Count > 1 ? ordered[1].Value : "";
            }

            if (!string.IsNullOrWhiteSpace(item.FullName) || !string.IsNullOrWhiteSpace(item.PhoneNumber))
                result.Rows.Add(item);
        }
    }

    private static void ParseDocx(byte[] bytes, FileParseResult result)
    {
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var docEntry = archive.GetEntry("word/document.xml");
        if (docEntry == null)
        {
            result.Errors.Add("Word dosyasinda document.xml bulunamadi.");
            return;
        }

        using var docStream = docEntry.Open();
        var doc = XDocument.Load(docStream);
        var ns = doc.Root!.GetDefaultNamespace();

        // Extract text from paragraphs
        var paragraphs = doc.Descendants(ns + "p");
        foreach (var para in paragraphs)
        {
            var text = string.Concat(para.Descendants(ns + "t").Select(t => t.Value)).Trim();
            if (string.IsNullOrEmpty(text)) continue;

            var item = ParseTextLine(text);
            if (item != null)
                result.Rows.Add(item);
        }
    }

    private static BulkContactItem? ParseTextLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        // Try common delimiters: tab, semicolon, comma, pipe
        char[] delimiters = { '\t', ';', '|' };
        foreach (var delim in delimiters)
        {
            if (line.Contains(delim))
            {
                var parts = line.Split(delim);
                if (parts.Length >= 2)
                {
                    // Check which part looks like a phone number
                    var phonePart = parts.FirstOrDefault(p => LooksLikePhone(p.Trim()));
                    var namePart = parts.FirstOrDefault(p => p != phonePart && !string.IsNullOrWhiteSpace(p));

                    if (phonePart != null)
                    {
                        return new BulkContactItem
                        {
                            FullName = namePart?.Trim() ?? "",
                            PhoneNumber = phonePart.Trim(),
                            Company = parts.Length > 2 ? parts[2].Trim() : null
                        };
                    }
                }
            }
        }

        // Single value per line - might be just a phone number
        var trimmed = line.Trim();
        if (LooksLikePhone(trimmed))
        {
            return new BulkContactItem { PhoneNumber = trimmed };
        }

        // Might be "Name - Phone" or "Name Phone" format
        var dashSplit = trimmed.Split(new[] { " - ", " – " }, StringSplitOptions.RemoveEmptyEntries);
        if (dashSplit.Length >= 2)
        {
            var phone = dashSplit.FirstOrDefault(s => LooksLikePhone(s.Trim()));
            var name = dashSplit.FirstOrDefault(s => s != phone);
            if (phone != null)
                return new BulkContactItem { FullName = name?.Trim() ?? "", PhoneNumber = phone.Trim() };
        }

        return null;
    }

    private static bool LooksLikePhone(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        var digits = s.Count(char.IsDigit);
        return digits >= 7 && digits <= 15 && s.All(c => char.IsDigit(c) || c == '+' || c == ' ' || c == '(' || c == ')' || c == '-');
    }

    private static string SanitizePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "";
        // Keep only digits and leading +
        var clean = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());
        if (clean.StartsWith("00")) clean = "+" + clean[2..];
        return clean.Length >= 7 ? clean : "";
    }

    private static string GetCellValue(XElement cell, XNamespace ns, List<string> sharedStrings)
    {
        var type = cell.Attribute("t")?.Value;
        var valueEl = cell.Element(ns + "v");
        if (valueEl == null) return "";

        if (type == "s" && int.TryParse(valueEl.Value, out var idx) && idx < sharedStrings.Count)
            return sharedStrings[idx];

        return valueEl.Value;
    }

    private static int ColumnRefToIndex(string cellRef)
    {
        int col = 0;
        foreach (var c in cellRef)
        {
            if (char.IsLetter(c))
                col = col * 26 + (char.ToUpper(c) - 'A' + 1);
            else
                break;
        }
        return col - 1;
    }

    private static bool IsNameHeader(string h) =>
        h.Contains("ad") || h.Contains("name") || h.Contains("isim") || h.Contains("soyad") || h == "kisi";

    private static bool IsPhoneHeader(string h) =>
        h.Contains("tel") || h.Contains("phone") || h.Contains("numara") || h.Contains("gsm") || h.Contains("cep") || h.Contains("mobile");

    private static bool IsCompanyHeader(string h) =>
        h.Contains("firma") || h.Contains("sirket") || h.Contains("company") || h.Contains("kurum");

    private static bool IsEmailHeader(string h) =>
        h.Contains("mail") || h.Contains("eposta") || h.Contains("e-posta");

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
