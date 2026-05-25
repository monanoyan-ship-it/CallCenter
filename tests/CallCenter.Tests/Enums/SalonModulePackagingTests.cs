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
    public void LoyaltyMarketingPackage_ShouldNotContainSessionPackages()
    {
        SalonModuleGroups.GetModules(SalonModuleGroups.Ids.LoyaltyMarketing)
            .Select(m => m.Id)
            .Should()
            .NotContain(SalonPortalModules.Ids.SlnPackages);
    }
}
