using CallCenter.Shared.Enums;

namespace CallCenter.Tests.Enums;

public class IntegrationEnumTests
{
    // ═══════════════════════════════════════════════════════════
    // IntegrationPlatforms
    // ═══════════════════════════════════════════════════════════

    [Theory]
    [InlineData(1, "GenericWebhook")]
    [InlineData(2, "Salesforce")]
    [InlineData(3, "HubSpot")]
    [InlineData(4, "Zendesk")]
    public void IntegrationPlatforms_GetById_ShouldReturnCorrect(int id, string expectedSystemName)
    {
        var result = IntegrationPlatforms.GetById(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.SystemName.Should().Be(expectedSystemName);
    }

    [Fact]
    public void IntegrationPlatforms_All_ShouldHave4Items()
    {
        IntegrationPlatforms.All.Count().Should().Be(4);
    }

    [Fact]
    public void IntegrationPlatforms_Ids_ShouldBeUnique()
    {
        var ids = IntegrationPlatforms.All.Select(x => x.Id).ToList();
        ids.Distinct().Count().Should().Be(ids.Count);
    }

    // ═══════════════════════════════════════════════════════════
    // WebhookEventTypes
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void WebhookEventTypes_All_ShouldHave15Items()
    {
        WebhookEventTypes.All.Count().Should().Be(15);
    }

    [Fact]
    public void WebhookEventTypes_CallEvents_ShouldHave6Items()
    {
        WebhookEventTypes.CallEvents.Count().Should().Be(6);
    }

    [Fact]
    public void WebhookEventTypes_AgentEvents_ShouldHave3Items()
    {
        WebhookEventTypes.AgentEvents.Count().Should().Be(3);
    }

    [Theory]
    [InlineData("call.started", 1)]
    [InlineData("call.ended", 3)]
    [InlineData("agent.status_changed", 10)]
    [InlineData("queue.updated", 20)]
    [InlineData("contact.created", 30)]
    public void WebhookEventTypes_GetBySystemName_ShouldWork(string systemName, int expectedId)
    {
        var result = WebhookEventTypes.GetBySystemName(systemName);

        result.Should().NotBeNull();
        result!.Id.Should().Be(expectedId);
    }

    [Fact]
    public void WebhookEventTypes_Ids_ShouldBeUnique()
    {
        var ids = WebhookEventTypes.All.Select(x => x.Id).ToList();
        ids.Distinct().Count().Should().Be(ids.Count);
    }

    [Fact]
    public void WebhookEventTypes_SystemNames_ShouldBeUnique()
    {
        var names = WebhookEventTypes.All.Select(x => x.SystemName).ToList();
        names.Distinct().Count().Should().Be(names.Count);
    }

    // ═══════════════════════════════════════════════════════════
    // ContactSyncDirections
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void ContactSyncDirections_All_ShouldHave4Items()
    {
        ContactSyncDirections.All.Count().Should().Be(4);
    }

    [Theory]
    [InlineData(0, "None")]
    [InlineData(1, "ToExternal")]
    [InlineData(2, "FromExternal")]
    [InlineData(3, "Bidirectional")]
    public void ContactSyncDirections_GetById_ShouldReturnCorrect(int id, string expectedSystemName)
    {
        var result = ContactSyncDirections.GetById(id);

        result.Should().NotBeNull();
        result!.SystemName.Should().Be(expectedSystemName);
    }
}
