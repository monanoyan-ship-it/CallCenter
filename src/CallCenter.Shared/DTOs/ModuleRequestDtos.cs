namespace CallCenter.Shared.DTOs;

/// <summary>Modul talep bilgisi</summary>
public class ModuleRequestDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int ModuleId { get; set; }
    public string? ModuleName { get; set; }
    public string? ModuleIcon { get; set; }
    public decimal? CatalogPrice { get; set; }
    public int RequestTypeId { get; set; } = 1;
    public string? RequestTypeName { get; set; }
    public int StatusId { get; set; }
    public string? StatusName { get; set; }
    public string? RequestNotes { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedByName { get; set; }
}

/// <summary>Yeni modul talep olusturma</summary>
public class CreateModuleRequestDto
{
    public int ModuleId { get; set; }
    public int RequestTypeId { get; set; } = 1;
    public string? Notes { get; set; }
}

/// <summary>Admin talep degerlendirme</summary>
public class ReviewModuleRequestDto
{
    public string? Notes { get; set; }
}
