using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace CallCenter.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString(args);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static string ResolveConnectionString(string[] args)
    {
        var explicitConnection = ReadConnectionArg(args);
        if (!string.IsNullOrWhiteSpace(explicitConnection))
            return explicitConnection;

        var envConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(envConnection))
            return ApplyDbPasswordOverride(envConnection);

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        var repoRoot = FindRepoRoot(Directory.GetCurrentDirectory());
        var apiConfigDir = Path.Combine(repoRoot, "src", "CallCenter.Api");

        var connectionString = ReadConnectionString(Path.Combine(apiConfigDir, "appsettings.json"));
        var environmentConnectionString = ReadConnectionString(Path.Combine(apiConfigDir, $"appsettings.{environment}.json"));

        connectionString = string.IsNullOrWhiteSpace(environmentConnectionString)
            ? connectionString
            : environmentConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection bulunamadi. " +
                "src/CallCenter.Api/appsettings.Development.json veya ConnectionStrings__DefaultConnection env degerini kontrol edin.");
        }

        return ApplyDbPasswordOverride(connectionString);
    }

    private static string? ReadConnectionArg(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--connection" && i + 1 < args.Length)
                return args[i + 1];

            const string prefix = "--connection=";
            if (args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return args[i][prefix.Length..];
        }

        return null;
    }

    private static string FindRepoRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git"))
                || File.Exists(Path.Combine(dir.FullName, "docker-compose.yml")))
                return dir.FullName;

            dir = dir.Parent;
        }

        return startPath;
    }

    private static string? ReadConnectionString(string path)
    {
        if (!File.Exists(path))
            return null;

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings)
            && connectionStrings.TryGetProperty("DefaultConnection", out var defaultConnection))
            return defaultConnection.GetString();

        return null;
    }

    private static string ApplyDbPasswordOverride(string connectionString)
    {
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        if (string.IsNullOrWhiteSpace(dbPassword))
            return connectionString;

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Password = dbPassword
        };
        return builder.ConnectionString;
    }
}
