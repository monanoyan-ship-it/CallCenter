namespace CallCenter.Shared.Entities;

public class SystemSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string ValueType { get; set; } = "string"; // string, int, bool, json
    public string? Description { get; set; }
    public bool IsSystem { get; set; } // true ise silinemez
}
