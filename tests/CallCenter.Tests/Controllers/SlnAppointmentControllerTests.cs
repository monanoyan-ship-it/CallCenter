using CallCenter.Api.Controllers;
using CallCenter.Api.Filters;

namespace CallCenter.Tests.Controllers;

public class SlnAppointmentControllerTests
{
    [Fact]
    public void NormalizeBranches_IsOwnerOnlyMaintenanceAction()
    {
        var method = typeof(SlnAppointmentController).GetMethod(nameof(SlnAppointmentController.NormalizeBranches));

        method.Should().NotBeNull();
        method!.GetCustomAttributes(typeof(RequireSalonOwnerAttribute), inherit: true)
            .Should().NotBeEmpty();
    }
}
