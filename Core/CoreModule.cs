using Microsoft.Extensions.DependencyInjection;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Pricing;
using GreenRetail.Core.Time;
using GreenRetail.Shared.Navigation;
using GreenRetail.Shared.State;

namespace GreenRetail.Core;

public static class CoreModule
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPricingPolicy, NigeriaCashPricingPolicy>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<INavigationService, ShellNavigationService>();

        return services;
    }
}