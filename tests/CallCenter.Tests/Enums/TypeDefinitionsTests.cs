using CallCenter.Shared.Enums;
using CallCenter.Shared.Entities;

namespace CallCenter.Tests.Enums;

/// <summary>
/// TypeDefinitions enum-like static class'larin tutarliligini test eder.
/// </summary>
public class TypeDefinitionsTests
{
    // ═══════════════════════════════════════
    // UserRoles
    // ═══════════════════════════════════════

    [Fact]
    public void UserRoles_AllContainsAllRoles()
    {
        UserRoles.All.Should().HaveCountGreaterThanOrEqualTo(4);
        UserRoles.All.Should().Contain(r => r.Id == UserRoles.Ids.Admin);
        UserRoles.All.Should().Contain(r => r.Id == UserRoles.Ids.Agent);
        UserRoles.All.Should().Contain(r => r.Id == UserRoles.Ids.Supervisor);
        UserRoles.All.Should().Contain(r => r.Id == UserRoles.Ids.CustomerUser);
    }

    [Fact]
    public void UserRoles_GetById_ReturnsCorrectRole()
    {
        var admin = UserRoles.GetById(UserRoles.Ids.Admin);
        admin.Should().NotBeNull();
        admin!.SystemName.Should().Be("Admin");
    }

    [Fact]
    public void UserRoles_GetById_InvalidId_ReturnsNull()
    {
        var result = UserRoles.GetById(99999);
        result.Should().BeNull();
    }

    // ═══════════════════════════════════════
    // AgentStatuses
    // ═══════════════════════════════════════

    [Fact]
    public void AgentStatuses_AllContainsAllStatuses()
    {
        AgentStatuses.All.Should().HaveCountGreaterThanOrEqualTo(5);
        AgentStatuses.All.Should().Contain(s => s.Id == AgentStatuses.Ids.Available);
        AgentStatuses.All.Should().Contain(s => s.Id == AgentStatuses.Ids.Busy);
        AgentStatuses.All.Should().Contain(s => s.Id == AgentStatuses.Ids.Offline);
    }

    // ═══════════════════════════════════════
    // SipPresenceStatuses — Bidirectional Mapping
    // ═══════════════════════════════════════

    [Fact]
    public void SipPresence_FromAgentStatus_AvailableMapsToOpen()
    {
        var sipStatus = SipPresenceStatuses.FromAgentStatus(AgentStatuses.Ids.Available);
        sipStatus.SystemName.Should().Be(SipPresenceStatuses.Online.SystemName);
    }

    [Fact]
    public void SipPresence_FromAgentStatus_OfflineMapsToClose()
    {
        var sipStatus = SipPresenceStatuses.FromAgentStatus(AgentStatuses.Ids.Offline);
        sipStatus.SystemName.Should().Be(SipPresenceStatuses.Offline.SystemName);
    }

    [Fact]
    public void SipPresence_ToAgentStatusId_RoundTrip()
    {
        // Available -> open -> Available
        var sipStatus = SipPresenceStatuses.FromAgentStatus(AgentStatuses.Ids.Available);
        var backToAgent = SipPresenceStatuses.ToAgentStatusId(sipStatus.SystemName);
        Assert.Equal(AgentStatuses.Ids.Available, backToAgent);
    }

    // ═══════════════════════════════════════
    // ForwardTypes
    // ═══════════════════════════════════════

    [Theory]
    [InlineData(ForwardTypes.Always, "Always")]
    [InlineData(ForwardTypes.Busy, "Busy")]
    [InlineData(ForwardTypes.NoAnswer, "NoAnswer")]
    [InlineData(ForwardTypes.Offline, "Offline")]
    public void ForwardTypes_GetName_ReturnsCorrectName(int type, string expected)
    {
        ForwardTypes.GetName(type).Should().Be(expected);
    }

    [Fact]
    public void ForwardTypes_GetName_UnknownType_ReturnsUnknown()
    {
        ForwardTypes.GetName(999).Should().Be("Unknown");
    }

    // ═══════════════════════════════════════
    // CrmContactSources
    // ═══════════════════════════════════════

    [Fact]
    public void CrmContactSources_HasAllSources()
    {
        CrmContactSources.All.Should().HaveCountGreaterThanOrEqualTo(3);
        CrmContactSources.All.Should().Contain(s => s.Id == CrmContactSources.Ids.Manual);
        CrmContactSources.All.Should().Contain(s => s.Id == CrmContactSources.Ids.LDAP);
        CrmContactSources.All.Should().Contain(s => s.Id == CrmContactSources.Ids.CSV);
    }

    // ═══════════════════════════════════════
    // AudioCodecs
    // ═══════════════════════════════════════

    [Fact]
    public void AudioCodecs_OpusExists()
    {
        var opus = AudioCodecs.GetById(AudioCodecs.Ids.Opus);
        opus.Should().NotBeNull();
    }

    // ═══════════════════════════════════════
    // VideoCodecs
    // ═══════════════════════════════════════

    [Fact]
    public void VideoCodecs_VP8Exists()
    {
        var vp8 = VideoCodecs.GetById(VideoCodecs.Ids.VP8);
        vp8.Should().NotBeNull();
    }

    [Fact]
    public void VideoCodecs_AllIds_AreUnique()
    {
        var ids = VideoCodecs.All.Select(v => v.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    // ═══════════════════════════════════════
    // MessageTypes
    // ═══════════════════════════════════════

    [Fact]
    public void MessageTypes_HasTextAndSystem()
    {
        MessageTypes.All.Should().Contain(m => m.Id == MessageTypes.Ids.Text);
        MessageTypes.All.Should().Contain(m => m.Id == MessageTypes.Ids.System);
    }
}
