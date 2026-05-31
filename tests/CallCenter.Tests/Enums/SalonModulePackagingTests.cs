using CallCenter.Shared.Enums;

namespace CallCenter.Tests.Enums;

public class SalonModulePackagingTests
{
    [Fact]
    public void SessionPackages_ShouldBeIncludedInCorePackage()
    {
        SalonPortalModules.SlnLoyaltyPackages.IsDefault.Should().BeTrue();
        SalonPortalModules.Defaults.Select(m => m.Id).Should().Contain(SalonPortalModules.Ids.SlnLoyaltyPackages);
        SalonModuleGroups.GetGroupId(SalonPortalModules.Ids.SlnLoyaltyPackages).Should().Be(SalonModuleGroups.Ids.Core);
    }

    [Fact]
    public void StockSupplyAndExpenses_ShouldBeIncludedInCorePackage()
    {
        SalonPortalModules.SlnSuppliers.IsDefault.Should().BeTrue();
        SalonPortalModules.SlnExpenses.IsDefault.Should().BeTrue();
        SalonModuleGroups.GetGroupId(SalonPortalModules.Ids.SlnSuppliers).Should().Be(SalonModuleGroups.Ids.Core);
        SalonModuleGroups.GetGroupId(SalonPortalModules.Ids.SlnExpenses).Should().Be(SalonModuleGroups.Ids.Core);
    }

    [Fact]
    public void ReportingModules_ShouldBeSoldAsOneReportingPackage()
    {
        SalonModuleGroups.All.Select(g => g.Id).Should()
            .BeEquivalentTo([SalonModuleGroups.Ids.Core, SalonModuleGroups.Ids.LoyaltyMarketing, SalonModuleGroups.Ids.Professional]);

        SalonModuleGroups.GetById(SalonModuleGroups.Ids.StockFinance).Should().BeNull();
        SalonModuleGroups.GetById(SalonModuleGroups.Ids.Enterprise).Should().BeNull();
        SalonModuleGroups.GetGroupId(SalonPortalModules.Ids.SlnBeforeAfter).Should().Be(SalonModuleGroups.Ids.Professional);
        SalonModuleGroups.GetGroupId(SalonPortalModules.Ids.SlnReports).Should().Be(SalonModuleGroups.Ids.Professional);
    }

    [Fact]
    public void SalonCrmService_ShouldNotContainSessionPackages()
    {
        SalonModuleGroups.GetModules(SalonModuleGroups.Ids.LoyaltyMarketing)
            .Select(m => m.Id)
            .Should()
            .NotContain(SalonPortalModules.Ids.SlnLoyaltyPackages);
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
            .Where(m => SalonModuleGroups.GetGroupId(m.Id) == SalonModuleGroups.Ids.LoyaltyMarketing)
            .Select(m => m.Id)
            .ToList();

        salonModuleIds.Should().NotBeEmpty();
        salonModuleIds.Should().OnlyContain(id => CrmModules.HasSalonModule(id));
    }

    [Fact]
    public void CrmCatalog_ShouldUseCrmNamespaceForNonCoreSalonModules()
    {
        var crmModules = SalonPortalModules.All
            .Where(m => SalonModuleGroups.GetGroupId(m.Id) == SalonModuleGroups.Ids.LoyaltyMarketing)
            .Select(m => m.Id)
            .Select(id => CrmModules.GetBySalonModuleId(id))
            .ToList();

        crmModules.Should().OnlyContain(m => m != null && m.SystemName.StartsWith("Crm"));
    }

    [Fact]
    public void CrmModuleGroups_ShouldExposeThreeCommercialContexts()
    {
        CrmModuleGroups.All.Select(g => g.Id).Should()
            .BeEquivalentTo([CrmModuleGroups.Ids.Core, CrmModuleGroups.Ids.Salon, CrmModuleGroups.Ids.CallCenter]);

        CrmModuleGroups.GetGroupId(CrmModules.Ids.Contacts).Should().Be(CrmModuleGroups.Ids.Core);
        CrmModuleGroups.GetGroupId(CrmModules.Ids.SalonGiftCards).Should().Be(CrmModuleGroups.Ids.Salon);
        CrmModuleGroups.GetGroupId(CrmModules.Ids.CallCenterTickets).Should().Be(CrmModuleGroups.Ids.CallCenter);
    }
}
