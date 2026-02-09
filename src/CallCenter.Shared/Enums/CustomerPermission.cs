namespace CallCenter.Shared.Enums;

[Flags]
public enum CustomerPermission
{
    None = 0,
    ViewDashboard = 1,
    ListenCalls = 2,
    MakeCalls = 4,
    ViewReports = 8,
    ManageAgents = 16,
    ManageQueues = 32,
    ManageSettings = 64,
    All = ViewDashboard | ListenCalls | MakeCalls | ViewReports | ManageAgents | ManageQueues | ManageSettings
}
