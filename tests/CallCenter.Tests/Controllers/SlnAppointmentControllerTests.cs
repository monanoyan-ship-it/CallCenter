using System.Security.Claims;
using CallCenter.Api.Controllers;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

    [Fact]
    public async Task GetAppointments_NonOwnerWithoutBranchClaim_Forbids()
    {
        var factory = Substitute.For<ISlnAppointmentFactory>();
        var controller = CreateController(factory, SalonRoles.Ids.BranchManager, branchId: null);

        var result = await controller.GetAppointments(null, null, null, null, null, branchId: 3);

        result.Result.Should().BeOfType<ForbidResult>();
        factory.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task GetAppointments_NonOwnerWithBranchClaim_OverridesRequestedBranch()
    {
        var factory = Substitute.For<ISlnAppointmentFactory>();
        factory.GetAppointmentsAsync(1, null, null, null, null, 3, null)
            .Returns(Task.FromResult(new List<SlnAppointmentDto>()));
        var controller = CreateController(factory, SalonRoles.Ids.BranchManager, branchId: 3);

        var result = await controller.GetAppointments(null, null, null, null, null, branchId: 9);

        result.Result.Should().BeOfType<OkObjectResult>();
        await factory.Received(1).GetAppointmentsAsync(1, null, null, null, null, 3, null);
    }

    [Fact]
    public async Task GetAppointments_OwnerCanUseRequestedBranchFilter()
    {
        var factory = Substitute.For<ISlnAppointmentFactory>();
        factory.GetAppointmentsAsync(1, null, null, null, null, 9, null)
            .Returns(Task.FromResult(new List<SlnAppointmentDto>()));
        var controller = CreateController(factory, SalonRoles.Ids.SalonOwner, branchId: null);

        var result = await controller.GetAppointments(null, null, null, null, null, branchId: 9);

        result.Result.Should().BeOfType<OkObjectResult>();
        await factory.Received(1).GetAppointmentsAsync(1, null, null, null, null, 9, null);
    }

    private static SlnAppointmentController CreateController(ISlnAppointmentFactory factory, int roleId, int? branchId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "7"),
            new("CustomerId", "1"),
            new("CustomerRoleId", roleId.ToString())
        };

        if (branchId.HasValue)
            claims.Add(new Claim("BranchId", branchId.Value.ToString()));

        return new SlnAppointmentController(factory)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
                }
            }
        };
    }
}
