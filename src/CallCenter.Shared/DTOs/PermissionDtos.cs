namespace CallCenter.Shared.DTOs;

/// <summary>Yetki tipi bilgisi (UI'da checkbox listesi icin)</summary>
public class PermissionTypeDto
{
    public int Id { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int ModuleId { get; set; }
    public string? ModuleName { get; set; }
}

/// <summary>Portal modulu bilgisi</summary>
public class PortalModuleDto
{
    public int Id { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public decimal CatalogPrice { get; set; }
    public decimal? CustomerPrice { get; set; }
    public decimal EffectivePrice => CustomerPrice ?? CatalogPrice;
    public DateTime? TrialEndsAt { get; set; }
    public List<PermissionTypeDto> Permissions { get; set; } = new();
}

/// <summary>Musteriye modul atama istegi</summary>
public class AssignModulesRequest
{
    public int[] ModuleIds { get; set; } = Array.Empty<int>();
    public string? Notes { get; set; }
    public decimal? MonthlyPrice { get; set; }
    public DateTime? TrialEndsAt { get; set; }
}
