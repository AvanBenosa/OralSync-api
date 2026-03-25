using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DMD.PERSISTENCE.Context;

public sealed class DmdDbContextFactory : IDesignTimeDbContextFactory<DmdDbContext>
{
    public DmdDbContext CreateDbContext(string[] args)
    {
        var settingsDirectory = ResolveSettingsDirectory();
        var environmentName = ResolveEnvironmentName();
        var connectionString = ReadConnectionString(settingsDirectory, environmentName)
            ?? throw new InvalidOperationException(
                $"Connection string 'Default' was not found for design-time DbContext creation. Environment: '{environmentName ?? "Base"}'."
            );

        var optionsBuilder = new DbContextOptionsBuilder<DmdDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new DmdDbContext(optionsBuilder.Options);
    }

    private static string ResolveSettingsDirectory()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            currentDirectory,
            Path.Combine(currentDirectory, "..", "DMD"),
            Path.Combine(currentDirectory, "DMD"),
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (File.Exists(Path.Combine(fullPath, "appsettings.json")))
            {
                return fullPath;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate appsettings.json for design-time DbContext creation."
        );
    }

    private static string? ResolveEnvironmentName()
    {
        var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (!string.IsNullOrWhiteSpace(aspNetCoreEnvironment))
        {
            return aspNetCoreEnvironment;
        }

        var dotNetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        if (!string.IsNullOrWhiteSpace(dotNetEnvironment))
        {
            return dotNetEnvironment;
        }

        return null;
    }

    private static string? ReadConnectionString(string settingsDirectory, string? environmentName)
    {
        var connectionString = ReadConnectionStringFromFile(Path.Combine(settingsDirectory, "appsettings.json"));

        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return connectionString;
        }

        var environmentSettingsPath = Path.Combine(
            settingsDirectory,
            $"appsettings.{environmentName}.json"
        );

        if (!File.Exists(environmentSettingsPath))
        {
            return connectionString;
        }

        return ReadConnectionStringFromFile(environmentSettingsPath) ?? connectionString;
    }

    private static string? ReadConnectionStringFromFile(string settingsPath)
    {
        using var stream = File.OpenRead(settingsPath);
        using var document = JsonDocument.Parse(stream);

        if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
        {
            return null;
        }

        if (!connectionStrings.TryGetProperty("Default", out var defaultConnection))
        {
            return null;
        }

        return defaultConnection.GetString();
    }
}
