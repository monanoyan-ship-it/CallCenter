using System.Text;
using CallCenter.Api.Hubs;
using CallCenter.Api.Services;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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

// Services
builder.Services.AddSingleton<AesEncryptionService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<CallDistributionService>();
builder.Services.AddSingleton<CallCenter.Shared.Services.ITranslationService, TranslationService>();

// Factory + Service pattern
builder.Services.AddScoped<IPortalService, PortalService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<ICallService, CallService>();
builder.Services.AddScoped<IQueueService, QueueService>();
builder.Services.AddScoped<ISipAccountService, SipAccountService>();
builder.Services.AddScoped<ISupervisorService, SupervisorService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ISettingService, SettingService>();
builder.Services.AddScoped<ITranslationManagementService, TranslationManagementService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();
builder.Services.AddScoped<ServiceFactory>();

// Background Services
builder.Services.AddHostedService<AuditPartitionMaintenanceService>();

// SignalR
builder.Services.AddSignalR();

// Controllers
builder.Services.AddControllers();

// OpenAPI / Swagger
builder.Services.AddOpenApi();

// CORS - Blazor WebAssembly için
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("https://localhost:7242", "http://localhost:5123")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Development: Swagger + Auto migrate
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Otomatik migration (development'ta)
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch
    {
        // Migration history bozuksa DB'yi sıfırla ve tekrar uygula
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazor");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CallCenterHub>("/hubs/callcenter");

app.Run();
