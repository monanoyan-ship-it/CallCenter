using System.Text;
using CallCenter.Api.DependencyInjection;
using CallCenter.Api.Helpers;
using CallCenter.Shared.Entities;
using CallCenter.Api.Hubs;
using CallCenter.Api.Middleware;
using CallCenter.Data;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// HttpClientFactory (WhatsApp API vb. dis servis cagrilari icin)
builder.Services.AddHttpClient();

// PostgreSQL + EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key yapilandirilmamis. appsettings.Development.json veya environment variable (Jwt__Key) ekleyin.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // SignalR için token'ı query string'den al
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Services — DI Registration (Infrastructure + EntityServices + Factories)
builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddEntityServices()
    .AddFactories();

// SignalR
builder.Services.AddSignalR();

// Controllers
builder.Services.AddControllers();

// OpenAPI / Swagger
builder.Services.AddOpenApi();

// CORS - Blazor WebAssembly + Docker/Windows App icin
var allowAllCors = builder.Configuration["Cors:AllowAll"] == "true";
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        if (allowAllCors)
        {
            // Docker/Test ortami: tum origin'lere izin ver
            // (Windows app farkli IP'den baglanacak)
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // Development: sadece bilinen origin'ler
            var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? new[] { "https://localhost:7242", "http://localhost:5123" };
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// OpenAPI (Development + Docker)
if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("AUTO_MIGRATE") == "true")
{
    app.MapOpenApi();
}

// Otomatik migration (Development veya Docker/test ortami)
// GUVENLIK: Hata durumunda ASLA EnsureDeletedAsync/EnsureCreatedAsync CAGIRMA — tum veriyi siler!
if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("AUTO_MIGRATE") == "true")
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var migrationLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Migration");
    try
    {
        var pending = await db.Database.GetPendingMigrationsAsync();
        var pendingList = pending.ToList();
        if (pendingList.Count > 0)
            migrationLogger.LogInformation("Bekleyen migration'lar uygulanacak: {Migrations}", string.Join(", ", pendingList));

        await db.Database.MigrateAsync();
        migrationLogger.LogInformation("Migration basarili.");
    }
    catch (Exception ex)
    {
        migrationLogger.LogCritical(ex, "MIGRATION HATASI! Veritabanina dokunulmadi. Manuel mudahale gerekiyor.");

        if (!app.Environment.IsDevelopment())
        {
            // Production/Docker: migration basarisizsa uygulama BASLAMASIN (fail fast)
            throw;
        }
        // Development: hata loglanir, gelistirici gorup duzeltir
    }
}

// Salon default data: Mevcut salon musterilerine eksik default verileri ekle
if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("AUTO_MIGRATE") == "true")
{
    using var seedScope = app.Services.CreateScope();
    var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seedLogger = seedScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SalonSeed");
    try
    {
        var salonCustomerIds = await seedDb.Customers
            .Where(c => c.ProductTypeId == ProductTypes.Ids.Salon && c.IsActive)
            .Select(c => c.Id)
            .ToListAsync();

        foreach (var cid in salonCustomerIds)
        {
            await SalonDefaultDataHelper.SeedDefaultDataAsync(seedDb, cid);

            // Eksik portal modullerini ekle
            var existingModuleIds = await seedDb.Set<CustomerPortalModule>()
                .Where(m => m.CustomerId == cid)
                .Select(m => m.ModuleId)
                .ToListAsync();

            foreach (var module in SalonPortalModules.All)
            {
                if (!existingModuleIds.Contains(module.Id))
                {
                    seedDb.Set<CustomerPortalModule>().Add(new CustomerPortalModule
                    {
                        CustomerId = cid,
                        ModuleId = module.Id,
                        IsActive = module.IsDefault,
                        ActivatedAt = DateTime.UtcNow
                    });
                }
            }
            await seedDb.SaveChangesAsync();
        }

        if (salonCustomerIds.Count > 0)
            seedLogger.LogInformation("Salon default data + modul kontrolu tamamlandi: {Count} salon", salonCustomerIds.Count);
    }
    catch (Exception ex)
    {
        seedLogger.LogWarning(ex, "Salon default data seed hatasi (kritik degil)");
    }
}

// HTTPS redirect — sadece Development ortaminda
// Docker'da Nginx arkasinda HTTP kullanilir, redirect gereksiz
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowBlazor");
app.UseAuthentication();
app.UseApiKeyAuth(); // Dis sistem API key dogrulamasi (/api/integration/v1/*)
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapControllers();
app.MapHub<CallCenterHub>("/hubs/callcenter");

// Sunucu basladiginda tum kullanicilari Offline'a cek (onceki oturumdan kalan stale status temizligi)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var resetCount = await db.Users
        .Where(u => u.StatusId != AgentStatuses.Ids.Offline)
        .ExecuteUpdateAsync(u => u.SetProperty(x => x.StatusId, AgentStatuses.Ids.Offline));
    if (resetCount > 0)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        logger.LogInformation("Startup: {Count} kullanicinin statusu Offline'a sifirlandi.", resetCount);
    }
}

app.Run();
