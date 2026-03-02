using CallCenter.Api.Services.Interfaces;

namespace CallCenter.Api.Services;

public class ServiceFactory
{
    private readonly IServiceProvider _sp;

    public ServiceFactory(IServiceProvider sp) => _sp = sp;

    public IPortalService CreatePortalService()
        => _sp.GetRequiredService<IPortalService>();

    public IAuthService CreateAuthService()
        => _sp.GetRequiredService<IAuthService>();

    public IUserService CreateUserService()
        => _sp.GetRequiredService<IUserService>();

    public ICustomerService CreateCustomerService()
        => _sp.GetRequiredService<ICustomerService>();

    public IAgentService CreateAgentService()
        => _sp.GetRequiredService<IAgentService>();

    public ICallService CreateCallService()
        => _sp.GetRequiredService<ICallService>();

    public IQueueService CreateQueueService()
        => _sp.GetRequiredService<IQueueService>();

    public ISipAccountService CreateSipAccountService()
        => _sp.GetRequiredService<ISipAccountService>();

    public ISupervisorService CreateSupervisorService()
        => _sp.GetRequiredService<ISupervisorService>();

    public IReportService CreateReportService()
        => _sp.GetRequiredService<IReportService>();

    public ISettingService CreateSettingService()
        => _sp.GetRequiredService<ISettingService>();

    public ITranslationManagementService CreateTranslationManagementService()
        => _sp.GetRequiredService<ITranslationManagementService>();

    public IOrganizationService CreateOrganizationService()
        => _sp.GetRequiredService<IOrganizationService>();

    public IAuditService CreateAuditService()
        => _sp.GetRequiredService<IAuditService>();

    public IAuditLogService CreateAuditLogService()
        => _sp.GetRequiredService<IAuditLogService>();

    public ICallForwardingService CreateCallForwardingService()
        => _sp.GetRequiredService<ICallForwardingService>();

    public IConferenceService CreateConferenceService()
        => _sp.GetRequiredService<IConferenceService>();

    public IMonitoringService CreateMonitoringService()
        => _sp.GetRequiredService<IMonitoringService>();

    public ICloudStorageService CreateCloudStorageService()
        => _sp.GetRequiredService<ICloudStorageService>();

    public IIvrService CreateIvrService()
        => _sp.GetRequiredService<IIvrService>();
}
