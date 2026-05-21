using CallCenter.Api.Controllers;
using CallCenter.Api.Filters;

namespace CallCenter.Tests.Controllers;

public class SlnWaitlistControllerTests
{
    [Fact]
    public void NormalizeBranches_IsOwnerOnlyMaintenanceAction()
    {
        var method = typeof(SlnWaitlistController).GetMethod(nameof(SlnWaitlistController.NormalizeBranches));

        method.Should().NotBeNull();
        method!.GetCustomAttributes(typeof(RequireSalonOwnerAttribute), inherit: true)
            .Should().NotBeEmpty();
    }
}
