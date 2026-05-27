using CallCenter.Shared.Enums;

namespace CallCenter.Tests.Enums;

public class SalonModulePackagingTests
{
    [Fact]
    public void SessionPackages_ShouldBeIncludedInCorePackage()
    {
        SalonPortalModules.SlnPackages.IsDefault.Should().BeTrue();
        SalonPortalModules.Defaults.Select(m => m.Id).Should().Contain(SalonPortalModules.Ids.SlnPackages);
        SalonModuleGroups.GetGroupId(SalonPortalModules.Ids.SlnPackages).Should().Be(SalonModuleGroups.Ids.Core);
    }

    [Fact]
    public void SalonCrmService_ShouldNotContainSessionPackages()
    {
        SalonModuleGroups.GetModules(SalonModuleGroups.Ids.LoyaltyMarketing)
            .Select(m => m.Id)
            .Should()
            .NotContain(SalonPortalModules.Ids.SlnPackages);
    }

    [Fact]
    public void SalonCrmService_ShouldHaveCrmDisplayName()
    {
        SalonModuleGroups.GetById(SalonModuleGroups.Ids.LoyaltyMarketing)!
            .Description
            .Should()
            .Contain("CRM");
    }

    [Fact]
    public void CrmCatalog_ShouldCoverAllNonCoreSalonModules()
    {
        var salonModuleIds = SalonPortalModules.All
            .Where(m => SalonModuleGroups.GetGroupId(m.Id) != SalonModuleGroups.Ids.Core)
            .Select(m => m.Id)
            .ToList();

        salonModuleIds.Should().NotBeEmpty();
        salonModuleIds.Should().OnlyContain(id => CrmModules.HasSalonModule(id));
    }

    [Fact]
    public void CrmCatalog_ShouldUseCrmNamespaceForNonCoreSalonModules()
    {
        var crmModules = SalonPortalModules.All
            .Where(m => SalonModuleGroups.GetGroupId(m.Id) != SalonModuleGroups.Ids.Core)
            .Select(m => m.Id)
            .Select(id => CrmModules.GetBySalonModuleId(id))
            .ToList();

        crmModules.Should().OnlyContain(m => m != null && m.SystemName.StartsWith("Crm"));
    }
}
