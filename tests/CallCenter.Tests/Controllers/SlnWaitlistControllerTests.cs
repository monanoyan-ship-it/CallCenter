using System.Security.Claims;
using CallCenter.Api.Controllers;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

    [Fact]
    public async Task WaitlistActions_NonOwnerWithoutBranchClaim_ForbidBeforeFactory()
    {
        var factory = Substitute.For<ISlnWaitlistFactory>();
        var controller = CreateController(factory, SalonRoles.Ids.BranchManager, branchId: null);
        var createDto = new SlnWaitlistEntryCreateDto();
        var updateDto = new SlnWaitlistEntryUpdateDto();
        var convertDto = new SlnWaitlistConvertToAppointmentDto();

        (await controller.GetEntries(null, branchId: 9, scope: null, search: null)).Result.Should().BeOfType<ForbidResult>();
        (await controller.GetEntry(10)).Result.Should().BeOfType<ForbidResult>();
        (await controller.CreateEntry(createDto, branchId: 9)).Result.Should().BeOfType<ForbidResult>();
        (await controller.UpdateEntry(10, updateDto, branchId: 9)).Should().BeOfType<ForbidResult>();
        (await controller.UpdateStatus(10, SlnWaitlistStatuses.Ids.Notified)).Should().BeOfType<ForbidResult>();
        (await controller.ConvertToAppointment(10, convertDto, branchId: 9)).Result.Should().BeOfType<ForbidResult>();
        (await controller.DeleteEntry(10)).Should().BeOfType<ForbidResult>();
        factory.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task WaitlistActions_NonOwnerWithBranchClaim_UseClaimBranchScope()
    {
        var factory = Substitute.For<ISlnWaitlistFactory>();
        var controller = CreateController(factory, SalonRoles.Ids.BranchManager, branchId: 3);
        var createDto = new SlnWaitlistEntryCreateDto();
        var updateDto = new SlnWaitlistEntryUpdateDto();
        var convertDto = new SlnWaitlistConvertToAppointmentDto();
        var entry = new SlnWaitlistEntryDto { Id = 10 };
        var conversion = new SlnWaitlistConversionDto
        {
            WaitlistEntry = entry,
            Appointment = new SlnAppointmentDto { Id = 30 }
        };

        factory.GetEntriesAsync(1, null, 3, SlnWaitlistStatuses.ScopeAll, null)
            .Returns(Task.FromResult(new List<SlnWaitlistEntryDto> { entry }));
        factory.GetEntryAsync(10, 1, 3)
            .Returns(Task.FromResult<SlnWaitlistEntryDto?>(entry));
        factory.CreateEntryAsync(Arg.Any<SlnWaitlistEntryCreateDto>(), 1, 3)
            .Returns(Task.FromResult((true, (string?)null, (SlnWaitlistEntryDto?)entry)));
        factory.UpdateEntryAsync(10, Arg.Any<SlnWaitlistEntryUpdateDto>(), 1, 3)
            .Returns(Task.FromResult((true, (string?)null)));
        factory.UpdateStatusAsync(10, SlnWaitlistStatuses.Ids.Notified, 1, 3)
            .Returns(Task.FromResult((true, (string?)null)));
        factory.ConvertToAppointmentAsync(10, Arg.Any<SlnWaitlistConvertToAppointmentDto>(), 7, 1, 3)
            .Returns(Task.FromResult((true, (string?)null, (SlnWaitlistConversionDto?)conversion)));
        factory.DeleteEntryAsync(10, 1, 3)
            .Returns(Task.FromResult((true, (string?)null)));

        (await controller.GetEntries(null, branchId: 9, scope: null, search: null)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetEntry(10)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.CreateEntry(createDto, branchId: 9)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.UpdateEntry(10, updateDto, branchId: 9)).Should().BeOfType<OkResult>();
        (await controller.UpdateStatus(10, SlnWaitlistStatuses.Ids.Notified)).Should().BeOfType<OkResult>();
        (await controller.ConvertToAppointment(10, convertDto, branchId: 9)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.DeleteEntry(10)).Should().BeOfType<OkResult>();

        await factory.Received(1).GetEntriesAsync(1, null, 3, SlnWaitlistStatuses.ScopeAll, null);
        await factory.Received(1).GetEntryAsync(10, 1, 3);
        await factory.Received(1).CreateEntryAsync(Arg.Is<SlnWaitlistEntryCreateDto>(d => d.BranchId == 9), 1, 3);
        await factory.Received(1).UpdateEntryAsync(10, Arg.Is<SlnWaitlistEntryUpdateDto>(d => d.BranchId == 9), 1, 3);
        await factory.Received(1).UpdateStatusAsync(10, SlnWaitlistStatuses.Ids.Notified, 1, 3);
        await factory.Received(1).ConvertToAppointmentAsync(10, Arg.Is<SlnWaitlistConvertToAppointmentDto>(d => d.BranchId == 9), 7, 1, 3);
        await factory.Received(1).DeleteEntryAsync(10, 1, 3);
    }

    private static SlnWaitlistController CreateController(ISlnWaitlistFactory factory, int roleId, int? branchId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "7"),
            new("CustomerId", "1"),
            new("CustomerRoleId", roleId.ToString())
        };

        if (branchId.HasValue)
            claims.Add(new Claim("BranchId", branchId.Value.ToString()));

        return new SlnWaitlistController(factory)
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
