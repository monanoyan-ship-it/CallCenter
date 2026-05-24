namespace CallCenter.Shared.DTOs;

public static class SlnDataImportTypes
{
    public const string Clients = "clients";
    public const string Services = "services";
    public const string Personnel = "personnel";
    public const string Products = "products";

    public static readonly string[] All = [Clients, Services, Personnel, Products];
}

public class SlnDataImportTemplateDto
{
    public string ImportType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<SlnDataImportTemplateColumnDto> Columns { get; set; } = [];
}

public class SlnDataImportTemplateColumnDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string? Description { get; set; }
}

public class SlnDataImportPreviewDto
{
    public string ImportType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ReadyRows { get; set; }
    public int WarningRows { get; set; }
    public int DuplicateRows { get; set; }
    public int ErrorRows { get; set; }
    public int ImportedRows { get; set; }
    public int SkippedRows { get; set; }
    public List<string> DetectedColumns { get; set; } = [];
    public List<SlnDataImportTemplateColumnDto> ExpectedColumns { get; set; } = [];
    public List<SlnDataImportRowDto> Rows { get; set; } = [];
}

public class SlnDataImportRowDto
{
    public int RowNumber { get; set; }
    public string Status { get; set; } = "ready";
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string?> Raw { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string?> Normalized { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

