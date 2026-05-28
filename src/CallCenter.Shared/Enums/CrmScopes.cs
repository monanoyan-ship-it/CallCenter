namespace CallCenter.Shared.Enums;

public static class CrmScopes
{
    public const string Core = "core";
    public const string Salon = "salon";
    public const string CallCenter = "callcenter";
    public const string Overview = "overview";

    public static readonly CrmScopeDefinition CoreDefinition = new(Core, "CRM", "/Home/Core", "bi-person-lines-fill");
    public static readonly CrmScopeDefinition SalonDefinition = new(Salon, "Salon", "/Home/Salon", "bi-scissors");
    public static readonly CrmScopeDefinition CallCenterDefinition = new(CallCenter, "Call Center", "/Home/CallCenter", "bi-headset");

    public static IEnumerable<CrmScopeDefinition> All => new[] { CoreDefinition, SalonDefinition, CallCenterDefinition };

    public static CrmScopeDefinition? GetByKey(string key) =>
        All.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
}

public record CrmScopeDefinition(
    string Key,
    string Label,
    string DashboardUrl,
    string Icon);
