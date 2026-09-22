using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Data;

public static class DataModule
{
    public static IServiceCollection AddData(this IServiceCollection services)
    {
        services.AddDbContextFactory<PosDbContext>(options =>
        {
            options.UseSqlite(GetConnectionString(), sqlite =>
            {
                sqlite.MigrationsAssembly(typeof(PosDbContext).Assembly.FullName);
            });
            // EF Core 10 treats pending model changes as an exception by default.
            // Startup owns migration/recovery, so a stale development snapshot must
            // not prevent the migration pipeline from creating the database. The
            // initializer records this condition and verifies the resulting schema.
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton<IStartupService, StartupService>();

        return services;
    }

    public static string GetConnectionString()
    {
        return $"Data Source={GetDatabasePath()}";
    }

    public static string GetDatabasePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GreenRetail");

        Directory.CreateDirectory(dir);

        return Path.Combine(dir, "greenretail.db");
    }
}