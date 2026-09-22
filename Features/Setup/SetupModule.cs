using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Features.Setup;

public static class SetupModule
{
    public static IServiceCollection AddSetupFeature(this IServiceCollection services)
    {
        services.AddTransient<ISystemSetupUseCase, SystemSetupUseCase>();
        services.AddTransient<SetupViewModel>();
        services.AddTransient<SetupPage>();
        return services;
    }
}
