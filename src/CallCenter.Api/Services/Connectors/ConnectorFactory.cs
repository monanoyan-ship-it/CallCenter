using CallCenter.Shared.Enums;

namespace CallCenter.Api.Services.Connectors;

/// <summary>
/// Strategy pattern ile platform tipine gore dogru connector adapter'i secer.
/// CloudStorageFactory pattern'i ile ayni yaklasim.
/// </summary>
public class ConnectorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ConnectorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>Platform tipine gore connector adapter dondurur. GenericWebhook icin null.</summary>
    public IConnectorAdapter? GetAdapter(int platformTypeId)
    {
        return platformTypeId switch
        {
            IntegrationPlatforms.Ids.Salesforce => _serviceProvider.GetRequiredService<SalesforceConnectorAdapter>(),
            IntegrationPlatforms.Ids.HubSpot => _serviceProvider.GetRequiredService<HubSpotConnectorAdapter>(),
            IntegrationPlatforms.Ids.Zendesk => _serviceProvider.GetRequiredService<ZendeskConnectorAdapter>(),
            _ => null // GenericWebhook — adapter gerekmez
        };
    }
}
