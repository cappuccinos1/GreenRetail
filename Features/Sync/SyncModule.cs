using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Features.Sync;

public static class SyncModule
{
    public static IServiceCollection AddSyncFeature(this IServiceCollection services)
    {
        services.AddTransient<ISyncOutboxService, SyncOutboxService>();

        return services;
    }
}