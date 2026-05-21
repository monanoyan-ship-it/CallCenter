using System.Reflection;
using CallCenter.Api.Controllers;
using CallCenter.Api.Filters;
using CallCenter.Shared.Enums;

namespace CallCenter.Tests.Controllers;

public class PortalControllerModuleGateTests
{
    [Fact]
    public void PersonnelEndpointsRequireStaffModule()
    {
        var methodNames = new[]
        {
            nameof(PortalController.CheckUsername),
            nameof(PortalController.GetPersonnel),
            nameof(PortalController.CreatePersonnel),
            nameof(PortalController.UpdatePersonnel),
            nameof(PortalController.ResetPersonnelPassword),
            nameof(PortalController.UploadPersonnelPhoto),
            nameof(PortalController.SetReportsTo),
            nameof(PortalController.DeactivatePersonnel),
            nameof(PortalController.ReactivatePersonnel),
            nameof(PortalController.UnlockPersonnel),
            nameof(PortalController.GetPersonnelOps),
            nameof(PortalController.UpsertShift),
            nameof(PortalController.DeleteShift),
            nameof(PortalController.CreateLeave),
            nameof(PortalController.UpdateLeaveStatus),
            nameof(PortalController.UpsertTimesheet),
            nameof(PortalController.CreateAdvance),
            nameof(PortalController.GeneratePayroll),
        };

        foreach (var methodName in methodNames)
        {
            var method = typeof(PortalController).GetMethod(methodName);
            method.Should().NotBeNull();

            var attribute = method!.GetCustomAttribute<RequireModuleAttribute>();
            attribute.Should().NotBeNull($"{methodName} must fail closed when Staff module is disabled");
            ModuleId(attribute!).Should().Be(SalonPortalModules.Ids.SlnStaff);
        }
    }

    private static int ModuleId(RequireModuleAttribute attribute)
    {
        var field = typeof(RequireModuleAttribute).GetField(
            "_moduleId",
            BindingFlags.Instance | BindingFlags.NonPublic);

        return (int)(field?.GetValue(attribute) ?? 0);
    }
}
