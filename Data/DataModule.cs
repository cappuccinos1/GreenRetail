using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Data;

public static class DataModule
{
    public static IServiceCollection AddData(this IServiceCollection services)
    {
        services.AddDbContextFactory<PosDbContext>(options =>
        {
            options.UseSqlite(GetConnectionString());
        });

        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton<IStartupService, StartupService>();

        return services;
    }

    public static string GetConnectionString()
    {
        return $"Data Source={GetDbPath()}";
    }

    private static string GetDbPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GreenRetail");

        Directory.CreateDirectory(dir);

        return Path.Combine(dir, "greenretail.db");
    }
}