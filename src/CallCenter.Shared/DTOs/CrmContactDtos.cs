namespace CallCenter.Shared.DTOs;

public class CrmContactSimpleDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PhoneNumber2 { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public int SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
}

public class CreateContactRequest
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PhoneNumber2 { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
}

public class UpdateContactRequest : CreateContactRequest
{
    public bool IsFavorite { get; set; }
}

/// <summary>CSV import istegi</summary>
public class CsvImportRequest
{
    /// <summary>CSV icerigi (Base64 encoded)</summary>
    public string CsvContentBase64 { get; set; } = string.Empty;

    /// <summary>Ilk satir baslik mi</summary>
    public bool HasHeader { get; set; } = true;

    /// <summary>Sutun eslestirmesi: CSV sutun index → alan adi</summary>
    public Dictionary<int, string> ColumnMapping { get; set; } = new();
}

/// <summary>CSV import sonucu</summary>
public class CsvImportResult
{
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

// ═══════════════════════════════════════════════════════════════
// BULK IMPORT (Toplu ekleme)
// ═══════════════════════════════════════════════════════════════

public class BulkContactItem
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public bool IsValid => !string.IsNullOrWhiteSpace(FullName) && !string.IsNullOrWhiteSpace(PhoneNumber);
}

public class BulkImportRequest
{
    public List<BulkContactItem> CrmContacts { get; set; } = new();
}

public class BulkImportResult
{
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class FileParseRequest
{
    public string FileContentBase64 { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public class FileParseResult
{
    public List<BulkContactItem> Rows { get; set; } = new();
    public List<string> Headers { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>LDAP baglanti ayarlari</summary>
public class LdapConfigDto
{
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; } = 389;
    public bool UseSsl { get; set; }
    public string? BindDn { get; set; }
    public string? BindPassword { get; set; }
    public string BaseDn { get; set; } = string.Empty;
    public string SearchFilter { get; set; } = "(objectClass=person)";
}

/// <summary>LDAP sync sonucu</summary>
public class LdapSyncResult
{
    public int TotalFound { get; set; }
    public int AddedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DeletedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
