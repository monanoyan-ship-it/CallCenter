using System.Security.Claims;
using CallCenter.Api.Controllers;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Tests.Controllers;

public class SlnServiceControllerTests
{
    [Fact]
    public async Task ResourceActions_NonOwnerWithoutBranchClaim_ForbidBeforeFactory()
    {
        var factory = Substitute.For<ISlnServiceFactory>();
        var controller = CreateController(factory, SalonRoles.Ids.BranchManager, branchId: null);
        var dto = new SlnResourceCreateDto();

        (await controller.GetResources()).Result.Should().BeOfType<ForbidResult>();
        (await controller.CreateResource(dto)).Result.Should().BeOfType<ForbidResult>();
        (await controller.UpdateResource(10, dto)).Should().BeOfType<ForbidResult>();
        (await controller.DeleteResource(10)).Should().BeOfType<ForbidResult>();
        factory.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task ResourceActions_NonOwnerWithBranchClaim_UseClaimBranchScope()
    {
        var factory = Substitute.For<ISlnServiceFactory>();
        var controller = CreateController(factory, SalonRoles.Ids.BranchManager, branchId: 3);
        var dto = new SlnResourceCreateDto { BranchId = 9, Name = "Room" };

        factory.GetResourcesAsync(1, 3)
            .Returns(Task.FromResult(new List<SlnResourceDto>()));
        factory.CreateResourceAsync(Arg.Any<SlnResourceCreateDto>(), 1, 3)
            .Returns(Task.FromResult(new SlnResourceDto { Id = 10 }));
        factory.UpdateResourceAsync(10, Arg.Any<SlnResourceCreateDto>(), 1, 3)
            .Returns(Task.FromResult((true, (string?)null)));
        factory.DeleteResourceAsync(10, 1, 3)
            .Returns(Task.FromResult((true, (string?)null)));

        (await controller.GetResources()).Result.Should().BeOfType<OkObjectResult>();
        (await controller.CreateResource(dto)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.UpdateResource(10, dto)).Should().BeOfType<OkResult>();
        (await controller.DeleteResource(10)).Should().BeOfType<OkResult>();

        await factory.Received(1).GetResourcesAsync(1, 3);
        await factory.Received(1).CreateResourceAsync(Arg.Is<SlnResourceCreateDto>(d => d.BranchId == 9), 1, 3);
        await factory.Received(1).UpdateResourceAsync(10, Arg.Is<SlnResourceCreateDto>(d => d.BranchId == 9), 1, 3);
        await factory.Received(1).DeleteResourceAsync(10, 1, 3);
    }

    [Fact]
    public async Task ResourceActions_OwnerUsesGlobalScope()
    {
        var factory = Substitute.For<ISlnServiceFactory>();
        var controller = CreateController(factory, SalonRoles.Ids.SalonOwner, branchId: null);
        var dto = new SlnResourceCreateDto { BranchId = 9, Name = "Room" };

        factory.GetResourcesAsync(1, null)
            .Returns(Task.FromResult(new List<SlnResourceDto>()));
        factory.CreateResourceAsync(Arg.Any<SlnResourceCreateDto>(), 1, null)
            .Returns(Task.FromResult(new SlnResourceDto { Id = 10 }));
        factory.UpdateResourceAsync(10, Arg.Any<SlnResourceCreateDto>(), 1, null)
            .Returns(Task.FromResult((true, (string?)null)));
        factory.DeleteResourceAsync(10, 1, null)
            .Returns(Task.FromResult((true, (string?)null)));

        (await controller.GetResources()).Result.Should().BeOfType<OkObjectResult>();
        (await controller.CreateResource(dto)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.UpdateResource(10, dto)).Should().BeOfType<OkResult>();
        (await controller.DeleteResource(10)).Should().BeOfType<OkResult>();

        await factory.Received(1).GetResourcesAsync(1, null);
        await factory.Received(1).CreateResourceAsync(Arg.Is<SlnResourceCreateDto>(d => d.BranchId == 9), 1, null);
        await factory.Received(1).UpdateResourceAsync(10, Arg.Is<SlnResourceCreateDto>(d => d.BranchId == 9), 1, null);
        await factory.Received(1).DeleteResourceAsync(10, 1, null);
    }

    private static SlnServiceController CreateController(ISlnServiceFactory factory, int roleId, int? branchId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "7"),
            new("CustomerId", "1"),
            new("CustomerRoleId", roleId.ToString())
        };

        if (branchId.HasValue)
            claims.Add(new Claim("BranchId", branchId.Value.ToString()));

        return new SlnServiceController(factory)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) }
            }
        };
    }
}
