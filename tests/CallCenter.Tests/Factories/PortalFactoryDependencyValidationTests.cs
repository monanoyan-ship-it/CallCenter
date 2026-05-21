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
    public async Task CreatePersonnelAsync_CreatesInactiveWhenRequestedWithoutConsumingActiveLimit()
    {
        await SeedCustomersAsync();
        var customer = await _db.Customers.SingleAsync(c => c.Id == 1);
        customer.MaxUsers = 0;
        await _db.SaveChangesAsync();
        var factory = CreateFactory();

        var result = await factory.CreatePersonnelAsync(
            1,
            CreateDto(isActive: false),
            createdByUserId: 99);

        var created = result.Result.Should().BeOfType<PortalPersonnelListDto>().Which;
        var user = await _db.Users.AsNoTracking().SingleAsync(u => u.UserName == "new-user");
        var personnel = await _db.CustomerPersonnel.AsNoTracking().SingleAsync(p => p.UserId == user.Id);
        result.Success.Should().BeTrue();
        created.IsActive.Should().BeFalse();
        user.IsActive.Should().BeFalse();
        personnel.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePersonnelAsync_ActiveCreateStillRespectsMaxUsers()
    {
        await SeedCustomersAsync();
        var customer = await _db.Customers.SingleAsync(c => c.Id == 1);
        customer.MaxUsers = 0;
        await _db.SaveChangesAsync();
        var factory = CreateFactory();

        var result = await factory.CreatePersonnelAsync(
            1,
            CreateDto(isActive: true),
            createdByUserId: 99);

        result.Success.Should().BeFalse();
        result.Result.Should().Be("Maksimum kullanici limitine (0) ulasildi.");
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
    public async Task ResetPersonnelPasswordAsync_UpdatesPasswordWithoutTouchingProfile()
    {
        await SeedPersonnelAsync(branchId: 1);
        var user = await _db.Users.SingleAsync(u => u.Id == 10);
        var originalTitle = await _db.CustomerPersonnel
            .AsNoTracking()
            .Where(p => p.Id == 20)
            .Select(p => p.Title)
            .SingleAsync();
        user.MustChangePassword = true;
        user.FailedLoginCount = 3;
        user.LockedUntil = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();
        var passwordPolicy = Substitute.For<IPasswordPolicyFactory>();
        passwordPolicy.ValidatePassword(Arg.Any<string>()).Returns((true, Array.Empty<string>()));
        passwordPolicy.IsPasswordReusedAsync(Arg.Any<int>(), Arg.Any<string>()).Returns(false);
        passwordPolicy.RecordPasswordAsync(Arg.Any<int>(), Arg.Any<string>()).Returns(Task.CompletedTask);
        var factory = CreateFactory(passwordPolicy);

        var result = await factory.ResetPersonnelPasswordAsync(1, 20, "NewPassword1!");

        var reloadedUser = await _db.Users.AsNoTracking().SingleAsync(u => u.Id == 10);
        var personnel = await _db.CustomerPersonnel.AsNoTracking().SingleAsync(p => p.Id == 20);
        result.Success.Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("NewPassword1!", reloadedUser.PasswordHash).Should().BeTrue();
        reloadedUser.PasswordChangedAt.Should().NotBeNull();
        reloadedUser.MustChangePassword.Should().BeFalse();
        reloadedUser.FailedLoginCount.Should().Be(0);
        reloadedUser.LockedUntil.Should().BeNull();
        personnel.Title.Should().Be(originalTitle);
        personnel.BranchId.Should().Be(1);
        await passwordPolicy.Received(1).RecordPasswordAsync(10, Arg.Any<string>());
    }

    [Fact]
    public async Task ResetPersonnelPasswordAsync_ReturnsPolicyErrorWithoutMutating()
    {
        await SeedPersonnelAsync();
        var passwordPolicy = Substitute.For<IPasswordPolicyFactory>();
        passwordPolicy.ValidatePassword("weakpass").Returns((false, new[] { "weak password" }));
        passwordPolicy.IsPasswordReusedAsync(Arg.Any<int>(), Arg.Any<string>()).Returns(false);
        passwordPolicy.RecordPasswordAsync(Arg.Any<int>(), Arg.Any<string>()).Returns(Task.CompletedTask);
        var factory = CreateFactory(passwordPolicy);

        var result = await factory.ResetPersonnelPasswordAsync(1, 20, "weakpass");

        var reloadedUser = await _db.Users.AsNoTracking().SingleAsync(u => u.Id == 10);
        result.Success.Should().BeFalse();
        result.Error.Should().Be("weak password");
        reloadedUser.PasswordHash.Should().Be("hash");
        reloadedUser.PasswordChangedAt.Should().BeNull();
        await passwordPolicy.DidNotReceive().RecordPasswordAsync(Arg.Any<int>(), Arg.Any<string>());
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

    [Fact]
    public async Task GetPersonnelOpsAsync_FiltersPayrollsByRequestedPeriodRange()
    {
        await SeedPersonnelAsync();
        _db.SlnPayrolls.AddRange(
            new SlnPayroll { Id = 40, PersonnelId = 20, Year = 2026, Month = 4, NetPay = 400 },
            new SlnPayroll { Id = 41, PersonnelId = 20, Year = 2026, Month = 5, NetPay = 500 },
            new SlnPayroll { Id = 42, PersonnelId = 20, Year = 2026, Month = 6, NetPay = 600 },
            new SlnPayroll { Id = 43, PersonnelId = 20, Year = 2026, Month = 7, NetPay = 700 });
        await _db.SaveChangesAsync();
        var factory = CreateFactory(useRealOps: true);

        var ops = await factory.GetPersonnelOpsAsync(
            1,
            new DateTime(2026, 5, 21),
            new DateTime(2026, 6, 20));

        ops.Payrolls.Select(p => $"{p.Year}-{p.Month}").Should().Equal("2026-6", "2026-5");
    }

    [Fact]
    public async Task CreatePersonnelAdvanceAsync_RejectsFinalizedPayrollPeriod()
    {
        await SeedPersonnelAsync();
        _db.SlnPayrolls.Add(new SlnPayroll
        {
            Id = 60,
            PersonnelId = 20,
            Year = 2026,
            Month = 5,
            IsFinalized = true,
            NetPay = 1000
        });
        await _db.SaveChangesAsync();
        var factory = CreateFactory(useRealOps: true);

        var result = await factory.CreatePersonnelAdvanceAsync(
            1,
            new PortalAdvanceCreateDto
            {
                PersonnelId = 20,
                Amount = 250,
                AdvanceDate = new DateTime(2026, 5, 21)
            });

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Kesinlesmis bordro donemine avans eklenemez.");
        (await _db.SlnAdvances.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreatePersonnelAdvanceAsync_CreatesDateOnlyAdvanceForOpenPeriod()
    {
        await SeedPersonnelAsync();
        var factory = CreateFactory(useRealOps: true);

        var result = await factory.CreatePersonnelAdvanceAsync(
            1,
            new PortalAdvanceCreateDto
            {
                PersonnelId = 20,
                Amount = 250,
                AdvanceDate = new DateTime(2026, 5, 21, 14, 30, 0),
                Notes = "May advance"
            });

        var advance = await _db.SlnAdvances.AsNoTracking().SingleAsync();
        result.Success.Should().BeTrue();
        advance.Amount.Should().Be(250);
        advance.AdvanceDate.Should().Be(new DateTime(2026, 5, 21));
        advance.Notes.Should().Be("May advance");
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

    private static PortalPersonnelCreateDto CreateDto(int? branchId = null, bool isActive = true)
        => new()
        {
            UserName = "new-user",
            FullName = "New User",
            Email = "new@example.com",
            Password = "Password1!",
            Title = "Kuaför",
            CustomerRoleId = SalonRoles.Ids.Hairdresser,
            BranchId = branchId,
            IsActive = isActive
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

    private PortalFactory CreateFactory(IPasswordPolicyFactory? passwordPolicy = null, bool useRealOps = false)
    {
        var resolvedPasswordPolicy = passwordPolicy ?? Substitute.For<IPasswordPolicyFactory>();
        if (passwordPolicy == null)
        {
            resolvedPasswordPolicy.ValidatePassword(Arg.Any<string>()).Returns((true, Array.Empty<string>()));
            resolvedPasswordPolicy.IsPasswordReusedAsync(Arg.Any<int>(), Arg.Any<string>()).Returns(false);
            resolvedPasswordPolicy.RecordPasswordAsync(Arg.Any<int>(), Arg.Any<string>()).Returns(Task.CompletedTask);
        }
        var shiftEs = useRealOps
            ? new SlnPersonnelShiftEntityService(_db)
            : Substitute.For<ISlnPersonnelShiftEntityService>();
        var timesheetEs = useRealOps
            ? new SlnPersonnelTimesheetEntityService(_db)
            : Substitute.For<ISlnPersonnelTimesheetEntityService>();
        var payrollEs = useRealOps
            ? new SlnPayrollEntityService(_db)
            : Substitute.For<ISlnPayrollEntityService>();
        var advanceEs = useRealOps
            ? new SlnAdvanceEntityService(_db)
            : Substitute.For<ISlnAdvanceEntityService>();

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
            resolvedPasswordPolicy,
            new AesEncryptionService(config),
            new SlnPersonnelSkillEntityService(_db),
            new SlnBranchEntityService(_db),
            new SlnServiceEntityService(_db),
            Substitute.For<ISlnPersonnelCommissionEntityService>(),
            shiftEs,
            new SlnPersonnelLeaveEntityService(_db),
            timesheetEs,
            payrollEs,
            advanceEs,
            Substitute.For<ISlnInvoiceItemEntityService>(),
            new UnitOfWork(_db));
    }
}
