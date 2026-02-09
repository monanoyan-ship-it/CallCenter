using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<CallRecord> CallRecords => Set<CallRecord>();
    public DbSet<Queue> Queues => Set<Queue>();
    public DbSet<QueueAgent> QueueAgents => Set<QueueAgent>();
    public DbSet<SipAccount> SipAccounts => Set<SipAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.UserName).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.UserName).HasMaxLength(50).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasMaxLength(150).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(u => u.Extension).HasMaxLength(10);
        });

        // CallRecord
        modelBuilder.Entity<CallRecord>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.CallerNumber).HasMaxLength(50).IsRequired();
            e.Property(c => c.CalleeNumber).HasMaxLength(50).IsRequired();
            e.HasOne(c => c.Agent).WithMany(u => u.CallRecords).HasForeignKey(c => c.AgentId);
            e.HasOne(c => c.Queue).WithMany(q => q.CallRecords).HasForeignKey(c => c.QueueId);
            e.HasIndex(c => c.StartedAt);
        });

        // Queue
        modelBuilder.Entity<Queue>(e =>
        {
            e.HasKey(q => q.Id);
            e.Property(q => q.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(q => q.Name).IsUnique();
        });

        // QueueAgent (many-to-many)
        modelBuilder.Entity<QueueAgent>(e =>
        {
            e.HasKey(qa => new { qa.QueueId, qa.AgentId });
            e.HasOne(qa => qa.Queue).WithMany(q => q.QueueAgents).HasForeignKey(qa => qa.QueueId);
            e.HasOne(qa => qa.Agent).WithMany().HasForeignKey(qa => qa.AgentId);
        });

        // SipAccount
        modelBuilder.Entity<SipAccount>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).HasMaxLength(100).IsRequired();
            e.Property(s => s.Server).HasMaxLength(200).IsRequired();
            e.Property(s => s.Username).HasMaxLength(100).IsRequired();
            e.Property(s => s.Password).HasMaxLength(256).IsRequired();
            e.Property(s => s.Domain).HasMaxLength(200);
            e.Property(s => s.Transport).HasMaxLength(10);
        });

        // Seed: Varsayılan admin kullanıcısı
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            UserName = "admin",
            FullName = "System Admin",
            Email = "admin@callcenter.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = Shared.Enums.UserRole.Admin,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
