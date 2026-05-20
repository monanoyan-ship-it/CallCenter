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

    [Fact]
    public async Task AppointmentActions_NonOwnerWithoutBranchClaim_ForbidBeforeFactory()
    {
        var factory = Substitute.For<ISlnAppointmentFactory>();
        var controller = CreateController(factory, SalonRoles.Ids.BranchManager, branchId: null);
        var dto = new SlnAppointmentCreateDto();
        var date = new DateTime(2026, 5, 20);

        (await controller.GetAppointment(30)).Result.Should().BeOfType<ForbidResult>();
        (await controller.CreateAppointment(dto, branchId: 9)).Result.Should().BeOfType<ForbidResult>();
        (await controller.UpdateAppointment(30, dto, branchId: 9)).Should().BeOfType<ForbidResult>();
        (await controller.UpdateStatus(30, new SlnAppointmentStatusRequest { StatusId = 4 })).Should().BeOfType<ForbidResult>();
        (await controller.DeleteAppointment(30)).Should().BeOfType<ForbidResult>();
        (await controller.CheckConflict(11, date, date.AddHours(1), null)).Result.Should().BeOfType<ForbidResult>();
        (await controller.GetAvailableStaff("7,8", branchId: 9)).Should().BeOfType<ForbidResult>();
        (await controller.GetAvailableSlots(11, date, 30, "7", branchId: 9)).Should().BeOfType<ForbidResult>();
        factory.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task AppointmentActions_NonOwnerWithBranchClaim_UseClaimBranchScope()
    {
        var factory = Substitute.For<ISlnAppointmentFactory>();
        var controller = CreateController(factory, SalonRoles.Ids.BranchManager, branchId: 3);
        var dto = new SlnAppointmentCreateDto();
        var date = new DateTime(2026, 5, 20);

        factory.GetAppointmentAsync(30, 1, 3)
            .Returns(Task.FromResult<SlnAppointmentDto?>(new SlnAppointmentDto { Id = 30 }));
        factory.CreateAppointmentAsync(Arg.Any<SlnAppointmentCreateDto>(), 7, 1, 3)
            .Returns(Task.FromResult<(SlnAppointmentDto?, string?)>((new SlnAppointmentDto { Id = 31 }, null)));
        factory.UpdateAppointmentAsync(30, Arg.Any<SlnAppointmentCreateDto>(), 1, 3)
            .Returns(Task.FromResult((true, (string?)null)));
        factory.UpdateStatusAsync(30, 4, 1, 3)
            .Returns(Task.FromResult((true, (string?)null, 0m)));
        factory.DeleteAppointmentAsync(30, 1, 3)
            .Returns(Task.FromResult((true, (string?)null)));
        factory.CheckConflictAsync(11, date, date.AddHours(1), 1, null)
            .Returns(Task.FromResult(false));
        factory.GetAvailableStaffAsync(1, Arg.Any<List<int>>(), 3)
            .Returns(Task.FromResult(new List<object>()));
        factory.GetAvailableSlotsAsync(1, 11, date, 30, 3, Arg.Any<List<int>>())
            .Returns(Task.FromResult(new List<object>()));

        (await controller.GetAppointment(30)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.CreateAppointment(dto, branchId: 9)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.UpdateAppointment(30, dto, branchId: 9)).Should().BeOfType<OkResult>();
        (await controller.UpdateStatus(30, new SlnAppointmentStatusRequest { StatusId = 4 })).Should().BeOfType<OkObjectResult>();
        (await controller.DeleteAppointment(30)).Should().BeOfType<OkResult>();
        (await controller.CheckConflict(11, date, date.AddHours(1), null)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetAvailableStaff("7,8", branchId: 9)).Should().BeOfType<OkObjectResult>();
        (await controller.GetAvailableSlots(11, date, 30, "7", branchId: 9)).Should().BeOfType<OkObjectResult>();

        await factory.Received(1).GetAppointmentAsync(30, 1, 3);
        await factory.Received(1).CreateAppointmentAsync(Arg.Is<SlnAppointmentCreateDto>(d => d.BranchId == 9), 7, 1, 3);
        await factory.Received(1).UpdateAppointmentAsync(30, Arg.Is<SlnAppointmentCreateDto>(d => d.BranchId == 9), 1, 3);
        await factory.Received(1).UpdateStatusAsync(30, 4, 1, 3);
        await factory.Received(1).DeleteAppointmentAsync(30, 1, 3);
        await factory.Received(1).CheckConflictAsync(11, date, date.AddHours(1), 1, null);
        await factory.Received(1).GetAvailableStaffAsync(1, Arg.Is<List<int>>(ids => ids.SequenceEqual(new[] { 7, 8 })), 3);
        await factory.Received(1).GetAvailableSlotsAsync(1, 11, date, 30, 3, Arg.Is<List<int>>(ids => ids.SequenceEqual(new[] { 7 })));
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
