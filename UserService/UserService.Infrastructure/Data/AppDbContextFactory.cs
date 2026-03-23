using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.IO;
using System.Text.Json;

namespace UserService.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var settingsPath = FindAppSettingsPath();
        var connectionString = GetDefaultConnection(settingsPath);

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string FindAppSettingsPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        for (var i = 0; i < 5; i++)
        {
            var candidate = Path.Combine(currentDir, "appsettings.json");
            if (File.Exists(candidate))
                return candidate;

            var parent = Directory.GetParent(currentDir);
            if (parent == null)
                break;

            currentDir = parent.FullName;
        }

        throw new InvalidOperationException("Could not locate appsettings.json for DesignTimeDbContextFactory.");
    }

    private static string GetDefaultConnection(string appSettingsPath)
    {
        var json = File.ReadAllText(appSettingsPath);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connStrings) &&
            connStrings.TryGetProperty("DefaultConnection", out var defaultConn) &&
            defaultConn.ValueKind == JsonValueKind.String)
        {
            return defaultConn.GetString()!;
        }

        throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json.");
    }
}
