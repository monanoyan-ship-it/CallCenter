using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Tests.Helpers;

/// <summary>
/// InMemory DbContext factory — her test icin izole DB.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>Benzersiz isimle yeni InMemory DbContext olusturur.</summary>
    public static AppDbContext Create(string? dbName = null)
    {
        dbName ??= Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    /// <summary>Ortak test verileri ile hazir DbContext olusturur.</summary>
    public static AppDbContext CreateWithSeedData(string? dbName = null)
    {
        var db = Create(dbName);
        SeedTestData(db);
        return db;
    }

    private static void SeedTestData(AppDbContext db)
    {
        // Test kullanicilari
        var admin = new User
        {
            Id = 100,
            Uid = Guid.NewGuid(),
            UserName = "testadmin",
            FullName = "Test Admin",
            Email = "admin@test.local",
            PasswordHash = "$2a$11$test",
            RoleId = UserRoles.Ids.Admin,
            StatusId = AgentStatuses.Ids.Available,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var agent1 = new User
        {
            Id = 101,
            Uid = Guid.NewGuid(),
            UserName = "agent1",
            FullName = "Agent Bir",
            Email = "agent1@test.local",
            PasswordHash = "$2a$11$test",
            Extension = "1001",
            RoleId = UserRoles.Ids.Agent,
            StatusId = AgentStatuses.Ids.Available,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var agent2 = new User
        {
            Id = 102,
            Uid = Guid.NewGuid(),
            UserName = "agent2",
            FullName = "Agent Iki",
            Email = "agent2@test.local",
            PasswordHash = "$2a$11$test",
            Extension = "1002",
            RoleId = UserRoles.Ids.Agent,
            StatusId = AgentStatuses.Ids.Busy,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var supervisor = new User
        {
            Id = 103,
            Uid = Guid.NewGuid(),
            UserName = "supervisor1",
            FullName = "Supervisor Bir",
            Email = "supervisor@test.local",
            PasswordHash = "$2a$11$test",
            RoleId = UserRoles.Ids.Supervisor,
            StatusId = AgentStatuses.Ids.Available,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.AddRange(admin, agent1, agent2, supervisor);

        // Test musterisi
        var customer = new Customer
        {
            Id = 200,
            Uid = Guid.NewGuid(),
            Name = "Test Firma A.S.",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Customers.Add(customer);

        // Test SIP hesabi
        var sipAccount = new SipAccount
        {
            Id = 300,
            Uid = Guid.NewGuid(),
            CustomerId = 200,
            Name = "Test SIP",
            Server = "sip.test.local",
            Port = 5060,
            Username = "1001",
            Password = "encrypted_pass",
            Transport = "UDP",
            IsDefault = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.SipAccounts.Add(sipAccount);

        db.SaveChanges();
    }
}
