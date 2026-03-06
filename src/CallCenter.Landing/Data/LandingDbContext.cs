using Microsoft.EntityFrameworkCore;

namespace CallCenter.Landing.Data;

public class LandingDbContext : DbContext
{
    public LandingDbContext(DbContextOptions<LandingDbContext> options) : base(options) { }

    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<WaitlistEntry>(entity =>
        {
            entity.ToTable("WaitlistEntries");
            entity.HasIndex(w => w.Email).IsUnique();
        });
    }
}

public class WaitlistEntry
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}
