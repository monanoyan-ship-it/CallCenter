using CallCenter.Api.Services.Interfaces;

namespace CallCenter.Api.Services;

public class ServiceFactory
{
    private readonly IServiceProvider _sp;

    public ServiceFactory(IServiceProvider sp) => _sp = sp;

    public IPortalService CreatePortalService()
        => _sp.GetRequiredService<IPortalService>();
}
