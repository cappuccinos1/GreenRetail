using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Features.CashSessions;

public static class CashSessionsModule
{
    public static IServiceCollection AddCashSessionsFeature(this IServiceCollection services)
    {
        services.AddTransient<IOpenRegisterUseCase, OpenRegisterUseCase>();
        services.AddTransient<ICloseRegisterUseCase, CloseRegisterUseCase>();
        services.AddTransient<IGetActiveSessionQuery, GetActiveSessionQuery>();

        services.AddTransient<CashSessionViewModel>();
        services.AddTransient<CashSessionPage>();

        return services;
    }
}