using CallCenter.Api.EntityServices;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CallCenter.Tests.Factories;

public class PortalFactoryDependencyValidationTests : IDisposable
{
    private readonly AppDbContext _db;

    public PortalFactoryDependencyValidationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreatePersonnelAsync_RejectsBranchOutsideCustomerBeforeCreatingUser()
    {
        await SeedCustomersAsync();
        _db.SlnBranches.Add(new SlnBranch
        {
            Id = 30,
            CustomerId = 2,
            Name = "Other branch",
            IsActive = true
        });
        await _db.SaveChangesAsync();
        var factory = CreateFactory();

        var result = await factory.CreatePersonnelAsync(
            1,
            CreateDto(branchId: 30),
            createdByUserId: 99);

        result.Success.Should().BeFalse();
        result.Result.Should().Be("Şube bulunamadı.");
        (await _db.Users.AsNoTracking().AnyAsync(u => u.UserName == "new-user")).Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePersonnelAsync_RejectsReportsToOutsideCustomerWithoutMutating()
    {
        await SeedPersonnelAsync();
        _db.Users.Add(new User
        {
            Id = 11,
            UserName = "other-manager",
            FullName = "Other Manager",
            Email = "other@example.com",
            PasswordHash = "hash",
            RoleId = UserRoles.Ids.CustomerUser,
            IsActive = true
        });
        _db.CustomerPersonnel.Add(new CustomerPersonnel
        {
            Id = 21,
            UserId = 11,
            CustomerId = 2,
            Title = "Manager",
            CustomerRoleId = SalonRoles.Ids.Manager,
            IsActive = true
        });
        await _db.SaveChangesAsync();
        var factory = CreateFactory();

        var result = await factory.UpdatePersonnelAsync(
            1,
            20,
            UpdateDto(reportsToPersonnelId: 21));

        var personnel = await _db.CustomerPersonnel.AsNoTracking().SingleAsync(p => p.Id == 20);
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Amir personel bulunamadı.");
        personnel.ReportsToPersonnelId.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePersonnelAsync_RejectsSkillOutsideCustomerWithoutClearingExistingSkills()
    {
        await SeedPersonnelAsync();
        _db.SlnServices.AddRange(
            new SlnService
            {
                Id = 100,
                CustomerId = 1,
                CategoryId = 1,
                Name = "Cut",
                DurationMinutes = 30,
                Price = 100,
                IsActive = true
            },
            new SlnService
            {
                Id = 200,
                CustomerId = 2,
                CategoryId = 2,
                Name = "Other cut",
                DurationMinutes = 30,
                Price = 100,
                IsActive = true
            });
        _db.SlnPersonnelSkills.Add(new SlnPersonnelSkill { PersonnelId = 20, ServiceId = 100 });
        await _db.SaveChangesAsync();
        var factory = CreateFactory();

        var result = await factory.UpdatePersonnelAsync(
            1,
            20,
            UpdateDto(skillServiceIds: [200]));

        var skills = await _db.SlnPersonnelSkills.AsNoTracking().Where(s => s.PersonnelId == 20).ToListAsync();
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Hizmet yetenekleri bu salona ait olmalı.");
        skills.Should().ContainSingle();
        skills.Single().ServiceId.Should().Be(100);
    }

    [Fact]
    public async Task UpdatePersonnelAsync_AllowsClearingTitleWithBlankValue()
    {
        await SeedPersonnelAsync();
        var factory = CreateFactory();

        var result = await factory.UpdatePersonnelAsync(1, 20, UpdateDto(title: ""));

        var personnel = await _db.CustomerPersonnel.AsNoTracking().SingleAsync(p => p.Id == 20);
        result.Success.Should().BeTrue();
        personnel.Title.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdatePersonnelAsync_PreservesTitleWhenOmitted()
    {
        await SeedPersonnelAsync();
        var factory = CreateFactory();

        var result = await factory.UpdatePersonnelAsync(1, 20, UpdateDto(title: null));

        var personnel = await _db.CustomerPersonnel.AsNoTracking().SingleAsync(p => p.Id == 20);
        result.Success.Should().BeTrue();
        personnel.Title.Should().Be("Kuaför");
    }

    [Fact]
    public async Task UpdatePersonnelLeaveStatusAsync_RejectsBranchManagerOutsideBranch()
    {
        await SeedPersonnelAsync();
        _db.Users.Add(new User
        {
            Id = 12,
            UserName = "veli",
            FullName = "Veli Veli",
            Email = "veli@example.com",
            PasswordHash = "hash",
            RoleId = UserRoles.Ids.CustomerUser,
            IsActive = true
        });
        _db.CustomerPersonnel.Add(new CustomerPersonnel
        {
            Id = 22,
            UserId = 12,
            CustomerId = 1,
            Title = "Kuaför",
            CustomerRoleId = SalonRoles.Ids.Hairdresser,
            BranchId = 2,
            IsActive = true
        });
        _db.SlnPersonnelLeaves.Add(new SlnPersonnelLeave
        {
            Id = 50,
            PersonnelId = 22,
            LeaveTypeId = SalonLeaveTypes.Ids.Annual,
            StatusId = SalonLeaveStatuses.Ids.Pending,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date
        });
        await _db.SaveChangesAsync();
        var factory = CreateFactory();

        var result = await factory.UpdatePersonnelLeaveStatusAsync(
            1,
            50,
            new PortalPersonnelLeaveStatusDto { StatusId = SalonLeaveStatuses.Ids.Approved },
            reviewedByPersonnelId: 20,
            callerRoleId: SalonRoles.Ids.BranchManager,
            callerBranchId: 1);

        var leave = await _db.SlnPersonnelLeaves.AsNoTracking().SingleAsync(l => l.Id == 50);
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Personel bulunamadi.");
        leave.StatusId.Should().Be(SalonLeaveStatuses.Ids.Pending);
        leave.ReviewedByPersonnelId.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePersonnelLeaveStatusAsync_AllowsBranchManagerInsideBranch()
    {
        await SeedPersonnelAsync(branchId: 1);
        _db.SlnPersonnelLeaves.Add(new SlnPersonnelLeave
        {
            Id = 50,
            PersonnelId = 20,
            LeaveTypeId = SalonLeaveTypes.Ids.Annual,
            StatusId = SalonLeaveStatuses.Ids.Pending,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date
        });
        await _db.SaveChangesAsync();
        var factory = CreateFactory();

        var result = await factory.UpdatePersonnelLeaveStatusAsync(
            1,
            50,
            new PortalPersonnelLeaveStatusDto { StatusId = SalonLeaveStatuses.Ids.Approved },
            reviewedByPersonnelId: 20,
            callerRoleId: SalonRoles.Ids.BranchManager,
            callerBranchId: 1);

        var leave = await _db.SlnPersonnelLeaves.AsNoTracking().SingleAsync(l => l.Id == 50);
        result.Success.Should().BeTrue();
        leave.StatusId.Should().Be(SalonLeaveStatuses.Ids.Approved);
        leave.ReviewedByPersonnelId.Should().Be(20);
    }

    private async Task SeedCustomersAsync()
    {
        _db.Customers.AddRange(
            new Customer
            {
                Id = 1,
                Uid = Guid.NewGuid(),
                Name = "Salon",
                MaxUsers = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Customer
            {
                Id = 2,
                Uid = Guid.NewGuid(),
                Name = "Other Salon",
                MaxUsers = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        await _db.SaveChangesAsync();
    }

    private async Task SeedPersonnelAsync(int? branchId = null)
    {
        await SeedCustomersAsync();
        _db.Users.Add(new User
        {
            Id = 10,
            UserName = "ali",
            FullName = "Ali Veli",
            Email = "ali@example.com",
            PasswordHash = "hash",
            RoleId = UserRoles.Ids.CustomerUser,
            IsActive = true
        });
        _db.CustomerPersonnel.Add(new CustomerPersonnel
        {
            Id = 20,
            UserId = 10,
            CustomerId = 1,
            Title = "Kuaför",
            CustomerRoleId = SalonRoles.Ids.Hairdresser,
            BranchId = branchId,
            IsActive = true
        });
        await _db.SaveChangesAsync();
    }

    private static PortalPersonnelCreateDto CreateDto(int? branchId = null)
        => new()
        {
            UserName = "new-user",
            FullName = "New User",
            Email = "new@example.com",
            Password = "Password1!",
            Title = "Kuaför",
            CustomerRoleId = SalonRoles.Ids.Hairdresser,
            BranchId = branchId
        };

    private static PortalPersonnelUpdateDto UpdateDto(
        string? title = "Kuaför",
        int? reportsToPersonnelId = null,
        List<int>? skillServiceIds = null)
        => new()
        {
            FullName = "Ali Veli",
            Email = "ali@example.com",
            Title = title,
            CustomerRoleId = SalonRoles.Ids.Hairdresser,
            IsActive = true,
            ReportsToPersonnelId = reportsToPersonnelId,
            SkillServiceIds = skillServiceIds
        };

    private PortalFactory CreateFactory()
    {
        var passwordPolicy = Substitute.For<IPasswordPolicyFactory>();
        passwordPolicy.ValidatePassword(Arg.Any<string>()).Returns((true, Array.Empty<string>()));
        passwordPolicy.IsPasswordReusedAsync(Arg.Any<int>(), Arg.Any<string>()).Returns(false);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "portal-dependency-tests"
            })
            .Build();

        return new PortalFactory(
            new CustomerEntityService(_db),
            new CustomerPersonnelEntityService(_db),
            Substitute.For<ICustomerPortalModuleEntityService>(),
            Substitute.For<ISipAccountEntityService>(),
            new UserEntityService(_db),
            Substitute.For<ICallRecordEntityService>(),
            passwordPolicy,
            new AesEncryptionService(config),
            new SlnPersonnelSkillEntityService(_db),
            new SlnBranchEntityService(_db),
            new SlnServiceEntityService(_db),
            Substitute.For<ISlnPersonnelCommissionEntityService>(),
            Substitute.For<ISlnPersonnelShiftEntityService>(),
            new SlnPersonnelLeaveEntityService(_db),
            Substitute.For<ISlnPersonnelTimesheetEntityService>(),
            Substitute.For<ISlnPayrollEntityService>(),
            Substitute.For<ISlnAdvanceEntityService>(),
            Substitute.For<ISlnInvoiceItemEntityService>(),
            new UnitOfWork(_db));
    }
}
