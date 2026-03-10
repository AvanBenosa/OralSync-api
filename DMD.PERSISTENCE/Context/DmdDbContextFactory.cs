using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DMD.PERSISTENCE.Context;

public sealed class DmdDbContextFactory : IDesignTimeDbContextFactory<DmdDbContext>
{
    public DmdDbContext CreateDbContext(string[] args)
    {
        var settingsPath = ResolveAppSettingsPath();
        var connectionString = ReadConnectionString(settingsPath)
            ?? throw new InvalidOperationException(
                "Connection string 'Default' was not found for design-time DbContext creation."
            );

        var optionsBuilder = new DbContextOptionsBuilder<DmdDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new DmdDbContext(optionsBuilder.Options);
    }

    private static string ResolveAppSettingsPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(currentDirectory, "appsettings.json"),
            Path.Combine(currentDirectory, "..", "DMD", "appsettings.json"),
            Path.Combine(currentDirectory, "DMD", "appsettings.json"),
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate appsettings.json for design-time DbContext creation."
        );
    }

    private static string? ReadConnectionString(string settingsPath)
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
