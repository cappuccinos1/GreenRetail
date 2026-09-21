using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.BackOffice;

public static class BackOfficeModule
{
    public static IServiceCollection AddBackOfficeFeature(this IServiceCollection services)
    {
        services.AddTransient<BackOfficeViewModel>();
        services.AddTransient<BackOfficePage>();

        return services;
    }
}