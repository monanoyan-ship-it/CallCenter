namespace CallCenter.Shared.DTOs;

/// <summary>Modul fiyat bilgisi (katalog + firma bazli)</summary>
public class ModulePricingDto
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsDefault { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public decimal MonthlyPrice { get; set; }
    public bool HasPricing { get; set; }
}

/// <summary>Tek modul fiyat guncelleme</summary>
public class UpdateModulePricingRequest
{
    public int ModuleId { get; set; }
    public decimal MonthlyPrice { get; set; }
}

/// <summary>Toplu modul fiyat guncelleme</summary>
public class BulkUpdateModulePricingRequest
{
    public List<UpdateModulePricingRequest> Prices { get; set; } = new();
}
