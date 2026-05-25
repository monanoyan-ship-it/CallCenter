using CallCenter.Api.Controllers;
using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Api.Filters;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Tests.Helpers;

namespace CallCenter.Tests.Factories;

public class ApiTenantAuthorizationTests : IDisposable
{
    private readonly AppDbContext _db;

    public ApiTenantAuthorizationTests()
    {
        _db = TestDbContextFactory.Create();
    }

    [Fact]
    public async Task QueueFactory_WithCustomerScope_DoesNotReturnOtherTenantQueue()
    {
        var (customerA, customerB, _, agentB) = SeedTwoTenants();
        var queue = new Queue
        {
            Id = 301,
            Name = "A Kuyruk",
            CustomerId = customerA.Id,
            Customer = customerA,
            IsActive = true
        };
        _db.Queues.Add(queue);
        await _db.SaveChangesAsync();

        var sut = new QueueFactory(new QueueEntityService(_db), new UserEntityService(_db), new UnitOfWork(_db));

        Assert.Null(await sut.GetByIdAsync(queue.Id, customerB.Id));

        var updateResult = await sut.UpdateAsync(queue.Id, new QueueUpdateDto
        {
            Name = "B guncelleme denemesi",
            MaxWaitTimeSeconds = 30,
            IsActive = true
        }, customerB.Id);
        Assert.False(updateResult.Success);

        var assignResult = await sut.AssignAgentAsync(queue.Id, new QueueAgentAssignDto
        {
            AgentId = agentB.Id
        }, customerA.Id);
        Assert.False(assignResult.Success);
        Assert.Contains("firmaya ait degil", assignResult.Error);
    }

    [Fact]
    public async Task CallForwardingFactory_WithCustomerScope_DoesNotReturnOrMutateOtherTenantRule()
    {
        var (customerA, customerB, agentA, _) = SeedTwoTenants();
        var rule = new CallForwardingRule
        {
            Id = 401,
            UserId = agentA.Id,
            User = agentA,
            CustomerId = customerA.Id,
            ForwardType = ForwardTypes.Always,
            Destination = "1001",
            IsActive = true
        };
        _db.CallForwardingRules.Add(rule);
        await _db.SaveChangesAsync();

        var sut = new CallForwardingFactory(new CallForwardingRuleEntityService(_db), new UserEntityService(_db), new UnitOfWork(_db));

        Assert.Null(await sut.GetByIdAsync(rule.Id, customerB.Id));

        var updateResult = await sut.UpdateAsync(rule.Id, new CallForwardingRuleUpdateDto
        {
            Destination = "9999"
        }, customerB.Id);
        Assert.False(updateResult.Success);

        var createResult = await sut.CreateAsync(new CallForwardingRuleCreateDto
        {
            UserId = agentA.Id,
            CustomerId = customerB.Id,
            ForwardType = ForwardTypes.Busy,
            Destination = "1002"
        }, customerB.Id);
        Assert.False(createResult.Success);
        Assert.Contains("Kullanici bu firmaya ait degil", createResult.Error);
    }

    [Fact]
    public void ProductAndServiceMutations_RequireSalonPage_WhileCatalogReadsRemainOpen()
    {
        var createProductGate = GetSalonPageGate<SlnProductController>(nameof(SlnProductController.CreateProduct));
        var createServiceGate = GetSalonPageGate<SlnServiceController>(nameof(SlnServiceController.CreateService));
        var getProductsGate = GetSalonPageGate<SlnProductController>(nameof(SlnProductController.GetProducts));
        var getServicesGate = GetSalonPageGate<SlnServiceController>(nameof(SlnServiceController.GetServices));

        Assert.Equal("Products", createProductGate?.PageName);
        Assert.Equal("Services", createServiceGate?.PageName);
        Assert.Null(getProductsGate);
        Assert.Null(getServicesGate);
    }

    private (Customer CustomerA, Customer CustomerB, User AgentA, User AgentB) SeedTwoTenants()
    {
        var customerA = new Customer { Id = 101, Name = "Tenant A", IsActive = true };
        var customerB = new Customer { Id = 102, Name = "Tenant B", IsActive = true };
        var agentA = new User
        {
            Id = 201,
            UserName = "agent-a",
            FullName = "Agent A",
            Email = "a@test.local",
            PasswordHash = "hash",
            RoleId = UserRoles.Ids.CustomerUser,
            IsActive = true
        };
        var agentB = new User
        {
            Id = 202,
            UserName = "agent-b",
            FullName = "Agent B",
            Email = "b@test.local",
            PasswordHash = "hash",
            RoleId = UserRoles.Ids.CustomerUser,
            IsActive = true
        };
        var personnelA = new CustomerPersonnel
        {
            Id = 211,
            UserId = agentA.Id,
            User = agentA,
            CustomerId = customerA.Id,
            Customer = customerA,
            CustomerRoleId = CustomerRoles.Ids.Operator,
            Title = "Operator",
            IsActive = true
        };
        var personnelB = new CustomerPersonnel
        {
            Id = 212,
            UserId = agentB.Id,
            User = agentB,
            CustomerId = customerB.Id,
            Customer = customerB,
            CustomerRoleId = CustomerRoles.Ids.Operator,
            Title = "Operator",
            IsActive = true
        };

        agentA.CustomerPersonnel = personnelA;
        agentB.CustomerPersonnel = personnelB;
        _db.Customers.AddRange(customerA, customerB);
        _db.Users.AddRange(agentA, agentB);
        _db.CustomerPersonnel.AddRange(personnelA, personnelB);
        _db.SaveChanges();

        return (customerA, customerB, agentA, agentB);
    }

    private static RequireSalonPageAttribute? GetSalonPageGate<TController>(string methodName)
    {
        var method = typeof(TController).GetMethod(methodName);
        Assert.NotNull(method);
        return method!.GetCustomAttributes(typeof(RequireSalonPageAttribute), inherit: true)
            .Cast<RequireSalonPageAttribute>()
            .SingleOrDefault();
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
