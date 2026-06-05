using System.Net;
using System.Reflection;
using System.Text;
using CallCenter.Api.DependencyInjection;
using CallCenter.Api.Helpers;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Security;
using CallCenter.Api.Hubs;
using CallCenter.Api.Middleware;
using CallCenter.Data;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Npgsql: DateTime Kind=Unspecified gelirse UTC kabul et (strict mode'da hata verir)
// Legacy behavior KAPALI — tum tarihler UTC olarak kaydedilir, gosterimde timezone cevrimi yapilir
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}

var dpKeysPath = Path.Combine(Path.GetTempPath(), "dp-keys-api");
Directory.CreateDirectory(dpKeysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("CallCenter.Api")
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(365));

// HttpClientFactory (WhatsApp API vb. dis servis cagrilari icin)
builder.Services.AddHttpClient();

// PostgreSQL + EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

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

// In-memory cache (auth rate limit, kisa surel cache senaryolari)
builder.Services.AddMemoryCache();

// Background jobs (PAY.5 trial expiry, abonelik askıya alma)
builder.Services.AddHostedService<CallCenter.Api.Services.SubscriptionExpiryHostedService>();

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
            // Development: yapilandirilmis origin'ler + Flutter Web / Vite vb. (localhost herhangi bir port)
            var configured = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? new[] { "https://localhost:7242", "http://localhost:5123" };
            policy.SetIsOriginAllowed(origin =>
                {
                    if (string.IsNullOrEmpty(origin)) return false;
                    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
                    if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                        return false;
                    // Flutter Web / Chrome: Origin bazen 127.0.0.1, ::1 veya ::ffff:127.0.0.1 olabilir
                    if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (IPAddress.TryParse(uri.Host, out var ip) && IPAddress.IsLoopback(ip))
                        return true;
                    return configured.Contains(origin);
                })
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

// Otomatik migration (her ortamda zorunlu — Development/Staging/Production).
// AUTO_MIGRATE=false setlenirse atlanir (acil mudahale icin kacis vanasi).
// GUVENLIK: Hata durumunda ASLA EnsureDeletedAsync/EnsureCreatedAsync CAGIRMA — tum veriyi siler!
var autoMigrateDisabled = Environment.GetEnvironmentVariable("AUTO_MIGRATE") == "false";
if (!autoMigrateDisabled)
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

// Salon default data: Mevcut salon musterilerine eksik default verileri ekle (her ortamda).
if (!autoMigrateDisabled)
{
    using var seedScope = app.Services.CreateScope();
    var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seedLogger = seedScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SalonSeed");
    try
    {
        // Platform email event ve sablonlari (idempotent)
        await CallCenter.Api.Helpers.PlatformEmailSeedHelper.SeedAsync(seedDb);

        var salonCustomerIds = await seedDb.CustomerProducts
            .Where(cp => cp.ProductTypeId == ProductTypes.Ids.Salon && cp.IsActive)
            .Select(cp => cp.CustomerId)
            .Distinct()
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

// Cloud Run / reverse proxy arkasinda X-Forwarded-Proto: https header'ini oku.
// KnownNetworks ve KnownProxies temizlenmezse sadece loopback proxy'lere guvenir;
// Cloud Run load balancer IP'si bu listede olmadiginda header yok sayilir.
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownIPNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);
app.UseGlobalExceptionHandling();
app.UseCorpLynkSecurityHeaders();

// CORS, redirect/auth'dan once (OPTIONS ve tarayıcı istekleri için tutarlı sıra)
app.Use(async (context, next) =>
{
    if (context.Request.Headers.ContainsKey("Access-Control-Request-Private-Network"))
        context.Response.Headers["Access-Control-Allow-Private-Network"] = "true";

    await next();
});
app.UseCors("AllowBlazor");
// Yerel dosya yukleme fallback'i yalnizca Development'ta kullanilir (prod'da GcsUploadService -> GCS).
// Cloud Run non-root 'app' kullanicisi /app altinda klasor olusturamaz; bu blok prod'da gereksiz ve startup'i cokertir.
if (app.Environment.IsDevelopment())
{
    var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "Data", "Uploads");
    Directory.CreateDirectory(uploadsPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });
}

// HTTPS redirect production icin.
// Local development HTTP portu token kaybi olmadan kullanabilsin.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseApiKeyAuth(); // Dis sistem API key dogrulamasi (/api/integration/v1/*)
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapGet("/api/version", (IHostEnvironment env) => Results.Ok(new
{
    name = "CallCenter.Api",
    version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
    environment = env.EnvironmentName,
    timestamp = DateTime.UtcNow
}));
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
// deploy-1776270045
