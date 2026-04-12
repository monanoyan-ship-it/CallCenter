using CallCenter.Landing.Conventions;
using CallCenter.Landing.Data;
using CallCenter.Shared.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl yapilandirilmamis. appsettings.json kontrol edin.");

builder.Services.AddRazorPages(options =>
{
    options.Conventions.Add(new CulturePageRouteModelConvention());
});

builder.Services.AddDbContext<LandingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAppLocalization(apiBaseUrl, module: "landing");

builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteOptions>(options =>
{
    options.ConstraintMap.Add("culture", typeof(CultureRouteConstraint));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAppLocalization();
app.UseRouting();

// Waitlist API endpoints
app.MapPost("/api/waitlist", async (HttpContext context, LandingDbContext db) =>
{
    var request = await context.Request.ReadFromJsonAsync<WaitlistJoinRequest>();
    if (request == null || string.IsNullOrWhiteSpace(request.Email))
        return Results.Ok(new { success = false, message = "Email is required." });

    var email = request.Email.Trim().ToLowerInvariant();

    var exists = await db.WaitlistEntries.AnyAsync(w => w.Email == email);
    if (exists)
        return Results.Ok(new { success = true, message = "You're already on the list!" });

    var entry = new WaitlistEntry
    {
        Email = email,
        Source = request.Source,
        IpAddress = context.Connection.RemoteIpAddress?.ToString(),
        UserAgent = context.Request.Headers.UserAgent.ToString(),
        CreatedAt = DateTime.UtcNow
    };

    db.WaitlistEntries.Add(entry);
    await db.SaveChangesAsync();

    return Results.Ok(new { success = true, message = "Welcome aboard!" });
});

app.MapGet("/api/waitlist/count", async (LandingDbContext db) =>
{
    var count = await db.WaitlistEntries.CountAsync();
    return Results.Ok(new { count });
});

app.MapRazorPages();

app.Run();

record WaitlistJoinRequest(string Email, string? Source);
