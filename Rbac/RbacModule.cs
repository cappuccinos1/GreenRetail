using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Rbac;

public static class RbacModule
{
    public static IServiceCollection AddRbacFeature(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationService, AuthorizationService>();

        services.AddTransient<IGetRbacExplorerDataQuery, GetRbacExplorerDataQuery>();
        services.AddTransient<IGrantPermissionOverrideUseCase, GrantPermissionOverrideUseCase>();

        services.AddTransient<RbacExplorerViewModel>();
        services.AddTransient<RbacExplorerPage>();

        return services;
    }
}