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
    public DbSet<CustomerPersonnelPermission> CustomerPersonnelPermissions => Set<CustomerPersonnelPermission>();
    public DbSet<CustomerPortalModule> CustomerPortalModules => Set<CustomerPortalModule>();
    public DbSet<CustomerUserType> CustomerUserTypes => Set<CustomerUserType>();
    public DbSet<CustomerUserTypePermission> CustomerUserTypePermissions => Set<CustomerUserTypePermission>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<TranslationKey> TranslationKeys => Set<TranslationKey>();
    public DbSet<Translation> Translations => Set<Translation>();
    public DbSet<CustomerOrganizationUnit> CustomerOrganizationUnits => Set<CustomerOrganizationUnit>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();

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

        // CustomerPersonnelPermission (dinamik yetki atamalari)
        modelBuilder.Entity<CustomerPersonnelPermission>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.PersonnelId, p.PermissionTypeId }).IsUnique();
            e.Property(p => p.Description).HasMaxLength(500);
            e.HasOne(p => p.Personnel)
             .WithMany(cp => cp.Permissions)
             .HasForeignKey(p => p.PersonnelId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.CreatedByUser)
             .WithMany()
             .HasForeignKey(p => p.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // CustomerPortalModule (musteriye acik moduller)
        modelBuilder.Entity<CustomerPortalModule>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.CustomerId, m.ModuleId }).IsUnique();
            e.Property(m => m.Notes).HasMaxLength(500);
            e.HasOne(m => m.Customer)
             .WithMany(c => c.PortalModules)
             .HasForeignKey(m => m.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CustomerUserType (musteri kullanici tipleri / sablon yetki setleri)
        modelBuilder.Entity<CustomerUserType>(e =>
        {
            e.HasKey(ut => ut.Id);
            e.HasIndex(ut => ut.Uid).IsUnique();
            e.Property(ut => ut.Name).HasMaxLength(100).IsRequired();
            e.Property(ut => ut.Description).HasMaxLength(500);
            e.HasIndex(ut => new { ut.CustomerId, ut.Name }).IsUnique();
            e.HasOne(ut => ut.Customer)
             .WithMany(c => c.UserTypes)
             .HasForeignKey(ut => ut.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CustomerUserTypePermission (kullanici tipi sablon yetkileri)
        modelBuilder.Entity<CustomerUserTypePermission>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.UserTypeId, p.PermissionTypeId }).IsUnique();
            e.HasOne(p => p.UserType)
             .WithMany(ut => ut.Permissions)
             .HasForeignKey(p => p.UserTypeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CustomerOrganizationUnit (organizasyon hiyerarsisi)
        modelBuilder.Entity<CustomerOrganizationUnit>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.Uid).IsUnique();
            e.Property(o => o.Name).HasMaxLength(200).IsRequired();
            e.Property(o => o.Code).HasMaxLength(50);
            e.Property(o => o.Address).HasMaxLength(500);
            e.Property(o => o.Phone).HasMaxLength(20);
            e.Property(o => o.Email).HasMaxLength(150);
            e.HasIndex(o => new { o.CustomerId, o.Name, o.ParentId }).IsUnique();
            e.HasOne(o => o.Customer)
             .WithMany(c => c.OrganizationUnits)
             .HasForeignKey(o => o.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            // Self-reference: ust birim
            e.HasOne(o => o.Parent)
             .WithMany(o => o.Children)
             .HasForeignKey(o => o.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
            // Yonetici personel (opsiyonel)
            e.HasOne(o => o.ManagerPersonnel)
             .WithMany()
             .HasForeignKey(o => o.ManagerPersonnelId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // CustomerPersonnel.UserTypeId (opsiyonel FK — tip silinirse null olur)
        modelBuilder.Entity<CustomerPersonnel>(e =>
        {
            e.HasKey(cp => cp.Id);
            e.HasIndex(cp => cp.Uid).IsUnique();
            e.Property(cp => cp.Title).HasMaxLength(100).IsRequired();
            e.HasOne(cp => cp.Customer)
             .WithMany(c => c.Personnel)
             .HasForeignKey(cp => cp.CustomerId);
            e.HasOne(cp => cp.UserType)
             .WithMany(ut => ut.Personnel)
             .HasForeignKey(cp => cp.UserTypeId)
             .OnDelete(DeleteBehavior.SetNull);
            // Organizasyon birimi (opsiyonel)
            e.HasOne(cp => cp.OrganizationUnit)
             .WithMany(o => o.Personnel)
             .HasForeignKey(cp => cp.OrganizationUnitId)
             .OnDelete(DeleteBehavior.SetNull);
            // Ust yonetici (self-reference)
            e.HasOne(cp => cp.ReportsToPersonnel)
             .WithMany(cp => cp.Subordinates)
             .HasForeignKey(cp => cp.ReportsToPersonnelId)
             .OnDelete(DeleteBehavior.SetNull);
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
            e.HasIndex(q => new { q.CustomerId, q.Name }).IsUnique();
            e.HasOne(q => q.Customer)
             .WithMany(c => c.Queues)
             .HasForeignKey(q => q.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(q => q.OrganizationUnit)
             .WithMany(o => o.Queues)
             .HasForeignKey(q => q.OrganizationUnitId)
             .OnDelete(DeleteBehavior.SetNull);
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
            e.Property(s => s.Password).HasMaxLength(512).IsRequired();
            e.Property(s => s.Domain).HasMaxLength(200);
            e.Property(s => s.Transport).HasMaxLength(10);
            e.HasOne(s => s.Customer)
             .WithMany(c => c.SipAccounts)
             .HasForeignKey(s => s.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.OrganizationUnit)
             .WithMany(o => o.SipAccounts)
             .HasForeignKey(s => s.OrganizationUnitId)
             .OnDelete(DeleteBehavior.SetNull);
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

        // SystemSetting
        modelBuilder.Entity<SystemSetting>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Key).HasMaxLength(100).IsRequired();
            e.HasIndex(s => s.Key).IsUnique();
            e.Property(s => s.Value).IsRequired();
            e.Property(s => s.Group).HasMaxLength(50).IsRequired();
            e.Property(s => s.ValueType).HasMaxLength(20).IsRequired();
            e.Property(s => s.Description).HasMaxLength(500);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(rt => rt.Id);
            e.Property(rt => rt.Token).HasMaxLength(256).IsRequired();
            e.HasIndex(rt => rt.Token).IsUnique();
            e.HasIndex(rt => rt.UserId);
            e.HasOne(rt => rt.User)
             .WithMany()
             .HasForeignKey(rt => rt.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            // Computed kolonlar EF'e bildirilir (DB'de kolon yok)
            e.Ignore(rt => rt.IsExpired);
            e.Ignore(rt => rt.IsRevoked);
            e.Ignore(rt => rt.IsActive);
        });

        // PasswordHistory (sifre tekrar kullanim engelleme)
        modelBuilder.Entity<PasswordHistory>(e =>
        {
            e.HasKey(ph => ph.Id);
            e.Property(ph => ph.PasswordHash).HasMaxLength(256).IsRequired();
            e.HasIndex(ph => new { ph.UserId, ph.CreatedAt });
            e.HasOne(ph => ph.User)
             .WithMany(u => u.PasswordHistories)
             .HasForeignKey(ph => ph.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog (KVKK / BDDK uyumlu denetim kaydi)
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Category).HasMaxLength(50).IsRequired();
            e.Property(a => a.Action).HasMaxLength(50).IsRequired();
            e.Property(a => a.UserName).HasMaxLength(100);
            e.Property(a => a.EntityType).HasMaxLength(100);
            e.Property(a => a.EntityId).HasMaxLength(50);
            e.Property(a => a.Description).HasMaxLength(1000).IsRequired();
            e.Property(a => a.IpAddress).HasMaxLength(50);
            e.Property(a => a.UserAgent).HasMaxLength(500);
            // Performans: sik sorgulanan kolonlara index
            e.HasIndex(a => a.CreatedAt);
            e.HasIndex(a => a.Category);
            e.HasIndex(a => a.UserId);
            e.HasIndex(a => a.CustomerId);
            e.HasIndex(a => new { a.EntityType, a.EntityId });
            // FK YOK — PostgreSQL partitioned tablolarda FK desteklenmiyor
            // UserId ve CustomerId sadece bilgi amacli (snapshot), referential integrity gerekmiyor
            e.Ignore(a => a.User);
            e.Ignore(a => a.Customer);
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

        // Varsayılan sistem ayarları
        SeedSystemSettings(modelBuilder);
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

    private static void SeedSystemSettings(ModelBuilder modelBuilder)
    {
        var settings = new (int id, string key, string value, string group, string valueType, string desc, bool isSystem)[]
        {
            // Genel
            (1, "app.name", "Call Center", "general", "string", "Uygulama adi", true),
            (2, "app.language", "tr", "general", "string", "Varsayilan dil", true),
            (3, "app.timezone", "Europe/Istanbul", "general", "string", "Zaman dilimi", true),
            (4, "app.date_format", "dd.MM.yyyy", "general", "string", "Tarih formati", true),

            // Guvenlik
            (10, "security.max_login_attempts", "5", "security", "int", "Maks hatali giris denemesi", true),
            (11, "security.lockout_minutes", "15", "security", "int", "Hesap kilitleme suresi (dk)", true),
            (12, "security.token_expire_minutes", "480", "security", "int", "JWT token suresi (dk)", true),
            (13, "security.password_min_length", "8", "security", "int", "Minimum sifre uzunlugu", true),
            (14, "security.password_history_count", "5", "security", "int", "Son kac sifre tekrar kullanilamaz", true),
            (15, "security.recording_retention_years", "10", "security", "int", "Ses kaydi saklama suresi (yil) — TTK md. 82", true),

            // SIP
            (20, "sip.default_transport", "UDP", "sip", "string", "Varsayilan SIP transport", true),
            (21, "sip.registration_timeout", "3600", "sip", "int", "SIP kayit suresi (sn)", true),
            (22, "sip.keep_alive_interval", "30", "sip", "int", "Keep-alive araligi (sn)", true),

            // Bildirim
            (30, "notification.sound_enabled", "true", "notification", "bool", "Bildirim sesi", false),
            (31, "notification.desktop_enabled", "true", "notification", "bool", "Masaustu bildirimi", false),
            (32, "notification.ring_duration", "30", "notification", "int", "Zil calma suresi (sn)", false),
        };

        foreach (var (id, key, value, group, valueType, desc, isSystem) in settings)
        {
            modelBuilder.Entity<SystemSetting>().HasData(new SystemSetting
            {
                Id = id,
                Key = key,
                Value = value,
                Group = group,
                ValueType = valueType,
                Description = desc,
                IsSystem = isSystem
            });
        }
    }
}
