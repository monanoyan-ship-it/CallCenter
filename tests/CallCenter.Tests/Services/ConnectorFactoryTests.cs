using CallCenter.Api.Services.Connectors;
using CallCenter.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CallCenter.Tests.Services;

public class ConnectorFactoryTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConnectorFactory _sut;

    public ConnectorFactoryTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<SalesforceConnectorAdapter>();
        services.AddScoped<HubSpotConnectorAdapter>();
        services.AddScoped<ZendeskConnectorAdapter>();
        _serviceProvider = services.BuildServiceProvider();

        _sut = new ConnectorFactory(_serviceProvider);
    }

    [Fact]
    public void GetAdapter_Salesforce_ShouldReturnSalesforceAdapter()
    {
        var adapter = _sut.GetAdapter(IntegrationPlatforms.Ids.Salesforce);

        adapter.Should().NotBeNull();
        adapter.Should().BeOfType<SalesforceConnectorAdapter>();
        adapter!.PlatformTypeId.Should().Be(IntegrationPlatforms.Ids.Salesforce);
    }

    [Fact]
    public void GetAdapter_HubSpot_ShouldReturnHubSpotAdapter()
    {
        var adapter = _sut.GetAdapter(IntegrationPlatforms.Ids.HubSpot);

        adapter.Should().NotBeNull();
        adapter.Should().BeOfType<HubSpotConnectorAdapter>();
        adapter!.PlatformTypeId.Should().Be(IntegrationPlatforms.Ids.HubSpot);
    }

    [Fact]
    public void GetAdapter_Zendesk_ShouldReturnZendeskAdapter()
    {
        var adapter = _sut.GetAdapter(IntegrationPlatforms.Ids.Zendesk);

        adapter.Should().NotBeNull();
        adapter.Should().BeOfType<ZendeskConnectorAdapter>();
        adapter!.PlatformTypeId.Should().Be(IntegrationPlatforms.Ids.Zendesk);
    }

    [Fact]
    public void GetAdapter_GenericWebhook_ShouldReturnNull()
    {
        var adapter = _sut.GetAdapter(IntegrationPlatforms.Ids.GenericWebhook);

        adapter.Should().BeNull();
    }

    [Fact]
    public void GetAdapter_UnknownPlatform_ShouldReturnNull()
    {
        var adapter = _sut.GetAdapter(999);

        adapter.Should().BeNull();
    }
}
