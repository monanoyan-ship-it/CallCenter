using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
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
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerPersonnel> CustomerPersonnel => Set<CustomerPersonnel>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<TranslationKey> TranslationKeys => Set<TranslationKey>();
    public DbSet<Translation> Translations => Set<Translation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Uid).IsUnique();
            e.HasIndex(u => u.UserName).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.UserName).HasMaxLength(50).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasMaxLength(150).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(u => u.Extension).HasMaxLength(10);
            e.HasOne(u => u.CustomerPersonnel)
             .WithOne(cp => cp.User)
             .HasForeignKey<CustomerPersonnel>(cp => cp.UserId);
        });

        // Customer
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Uid).IsUnique();
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(c => c.Name);
            e.Property(c => c.TaxNumber).HasMaxLength(20);
            e.Property(c => c.Address).HasMaxLength(500);
            e.Property(c => c.Phone).HasMaxLength(20);
            e.Property(c => c.Email).HasMaxLength(150);
        });

        // CustomerPersonnel
        modelBuilder.Entity<CustomerPersonnel>(e =>
        {
            e.HasKey(cp => cp.Id);
            e.HasIndex(cp => cp.Uid).IsUnique();
            e.Property(cp => cp.Title).HasMaxLength(100).IsRequired();
            e.HasOne(cp => cp.Customer)
             .WithMany(c => c.Personnel)
             .HasForeignKey(cp => cp.CustomerId);
        });

        // CallRecord
        modelBuilder.Entity<CallRecord>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Uid).IsUnique();
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
            e.HasIndex(q => q.Uid).IsUnique();
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
            e.HasIndex(s => s.Uid).IsUnique();
            e.Property(s => s.Name).HasMaxLength(100).IsRequired();
            e.Property(s => s.Server).HasMaxLength(200).IsRequired();
            e.Property(s => s.Username).HasMaxLength(100).IsRequired();
            e.Property(s => s.Password).HasMaxLength(256).IsRequired();
            e.Property(s => s.Domain).HasMaxLength(200);
            e.Property(s => s.Transport).HasMaxLength(10);
        });

        // Language
        modelBuilder.Entity<Language>(e =>
        {
            e.HasKey(l => l.Code);
            e.Property(l => l.Code).HasMaxLength(5);
            e.Property(l => l.Name).HasMaxLength(50).IsRequired();
        });

        // TranslationKey
        modelBuilder.Entity<TranslationKey>(e =>
        {
            e.HasKey(tk => tk.Id);
            e.Property(tk => tk.Key).HasMaxLength(200).IsRequired();
            e.HasIndex(tk => tk.Key).IsUnique();
            e.Property(tk => tk.Module).HasMaxLength(50).IsRequired();
            e.Property(tk => tk.Description).HasMaxLength(500);
        });

        // Translation
        modelBuilder.Entity<Translation>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Value).IsRequired();
            e.Property(t => t.UpdatedBy).HasMaxLength(100);
            e.HasOne(t => t.TranslationKey).WithMany(tk => tk.Translations).HasForeignKey(t => t.TranslationKeyId);
            e.HasOne(t => t.Language).WithMany(l => l.Translations).HasForeignKey(t => t.LanguageCode);
            e.HasIndex(t => new { t.TranslationKeyId, t.LanguageCode }).IsUnique();
        });

        // =============================================
        // SEED DATA
        // =============================================

        // Varsayılan admin kullanıcısı
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Uid = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            UserName = "admin",
            FullName = "System Admin",
            Email = "admin@callcenter.local",
            // Sifre: 1123Azs+-  (BCrypt hash sabitlesti - migration uyumlulugu icin)
            PasswordHash = "$2a$11$4NK5QRHYyKGuXY/Wr41bGOgqCOD1PDK.c1473NdyCowy2.HJswS72",
            RoleId = UserRoles.Ids.Admin,
            StatusId = AgentStatuses.Ids.Offline,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Varsayılan diller
        modelBuilder.Entity<Language>().HasData(
            new Language { Code = "tr", Name = "Türkçe", IsDefault = true, IsActive = true },
            new Language { Code = "en", Name = "English", IsDefault = false, IsActive = true }
        );

        // Varsayılan çeviri key'leri ve çeviriler
        SeedTranslations(modelBuilder);
    }

    private static void SeedTranslations(ModelBuilder modelBuilder)
    {
        var keys = new (int id, string key, string module, string desc, string tr, string en)[]
        {
            // Auth
            (1, "auth.login", "auth", "Login butonu", "Giriş Yap", "Sign In"),
            (2, "auth.logout", "auth", "Çıkış butonu", "Çıkış Yap", "Sign Out"),
            (3, "auth.username", "auth", "Kullanıcı adı", "Kullanıcı Adı", "Username"),
            (4, "auth.password", "auth", "Şifre", "Şifre", "Password"),
            (5, "auth.login_failed", "auth", "Hatalı giriş", "Kullanıcı adı veya şifre hatalı.", "Invalid username or password."),

            // Agent Status
            (10, "agent.status.available", "agent", "Müsait durumu", "Müsait", "Available"),
            (11, "agent.status.busy", "agent", "Meşgul durumu", "Meşgul", "Busy"),
            (12, "agent.status.on_break", "agent", "Mola durumu", "Molada", "On Break"),
            (13, "agent.status.in_call", "agent", "Çağrıda durumu", "Çağrıda", "In Call"),
            (14, "agent.status.offline", "agent", "Çevrimdışı durumu", "Çevrimdışı", "Offline"),
            (15, "agent.status.acw", "agent", "Çağrı sonrası iş", "Çağrı Sonrası", "After Call Work"),

            // Common
            (20, "common.save", "common", "Kaydet butonu", "Kaydet", "Save"),
            (21, "common.cancel", "common", "İptal butonu", "İptal", "Cancel"),
            (22, "common.delete", "common", "Sil butonu", "Sil", "Delete"),
            (23, "common.edit", "common", "Düzenle butonu", "Düzenle", "Edit"),
            (24, "common.search", "common", "Arama", "Ara", "Search"),
            (25, "common.loading", "common", "Yükleniyor", "Yükleniyor...", "Loading..."),
            (26, "common.yes", "common", "Evet", "Evet", "Yes"),
            (27, "common.no", "common", "Hayır", "Hayır", "No"),

            // Call
            (30, "call.incoming", "call", "Gelen çağrı", "Gelen Çağrı", "Incoming Call"),
            (31, "call.outgoing", "call", "Giden çağrı", "Giden Çağrı", "Outgoing Call"),
            (32, "call.hold", "call", "Beklet", "Beklet", "Hold"),
            (33, "call.transfer", "call", "Transfer", "Transfer", "Transfer"),
            (34, "call.hangup", "call", "Kapat", "Kapat", "Hang Up"),
            (35, "call.answer", "call", "Cevapla", "Cevapla", "Answer"),
            (36, "call.reject", "call", "Reddet", "Reddet", "Reject"),

            // Dashboard
            (40, "dashboard.title", "dashboard", "Dashboard başlığı", "Gösterge Paneli", "Dashboard"),
            (41, "dashboard.active_calls", "dashboard", "Aktif çağrı", "Aktif Çağrılar", "Active Calls"),
            (42, "dashboard.agents_online", "dashboard", "Online agent", "Çevrimiçi Temsilciler", "Agents Online"),
            (43, "dashboard.queue_waiting", "dashboard", "Kuyrukta bekleyen", "Kuyrukta Bekleyen", "Waiting in Queue"),
        };

        int translationId = 1;

        foreach (var (id, key, module, desc, tr, en) in keys)
        {
            modelBuilder.Entity<TranslationKey>().HasData(new TranslationKey
            {
                Id = id,
                Key = key,
                Module = module,
                Description = desc
            });

            modelBuilder.Entity<Translation>().HasData(
                new Translation
                {
                    Id = translationId++,
                    TranslationKeyId = id,
                    LanguageCode = "tr",
                    Value = tr,
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedBy = "system"
                },
                new Translation
                {
                    Id = translationId++,
                    TranslationKeyId = id,
                    LanguageCode = "en",
                    Value = en,
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedBy = "system"
                }
            );
        }
    }
}
